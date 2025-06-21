using FisFaturaAPI.Data;
using FisFaturaAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FisFaturaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalInvoices = await _context.Invoices.CountAsync();
            var totalFirms = await _context.Firms.CountAsync();

            var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var monthlyIncome = await _context.Invoices
                .Where(i => i.GelirGider == "Gelir" && i.FaturaTarihi.HasValue && i.FaturaTarihi.Value.Date >= firstDayOfMonth.Date && i.FaturaTarihi.Value.Date <= lastDayOfMonth.Date)
                .SumAsync(i => i.ToplamTutar) ?? 0;

            var monthlyExpense = await _context.Invoices
                .Where(i => i.GelirGider == "Gider" && i.FaturaTarihi.HasValue && i.FaturaTarihi.Value.Date >= firstDayOfMonth.Date && i.FaturaTarihi.Value.Date <= lastDayOfMonth.Date)
                .SumAsync(i => i.ToplamTutar) ?? 0;
            
            var pendingInvoices = await _context.Invoices.CountAsync(i => i.Durum == "Beklemede");

            var stats = new
            {
                TotalInvoices = totalInvoices,
                TotalFirms = totalFirms,
                MonthlyIncome = monthlyIncome,
                MonthlyExpense = monthlyExpense,
                PendingInvoices = pendingInvoices
            };

            return Ok(stats);
        }

        [HttpGet("monthly-summary")]
        public async Task<IActionResult> GetMonthlySummary()
        {
            var today = DateTime.Today;
            var sixMonthsAgo = today.AddMonths(-6);

            var summary = await _context.Invoices
                .Where(i => i.FaturaTarihi.HasValue && i.FaturaTarihi.Value >= sixMonthsAgo)
                .GroupBy(i => new { Year = i.FaturaTarihi.Value.Year, Month = i.FaturaTarihi.Value.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Income = g.Where(i => i.GelirGider == "Gelir").Sum(i => i.ToplamTutar ?? 0),
                    Expense = g.Where(i => i.GelirGider == "Gider").Sum(i => i.ToplamTutar ?? 0)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();
            
            // Boş ayları sunucu tarafında (in-memory) doldur
            var result = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                var date = today.AddMonths(-i);
                var monthData = summary.FirstOrDefault(s => s.Year == date.Year && s.Month == date.Month);
                
                result.Add(new
                {
                    Month = date.ToString("yyyy-MM"),
                    Income = monthData?.Income ?? 0,
                    Expense = monthData?.Expense ?? 0
                });
            }

            return Ok(result);
        }

        [HttpGet("recent-invoices")]
        public async Task<IActionResult> GetRecentInvoices()
        {
            var recentInvoices = await _context.Invoices
                .Include(i => i.FirmaGonderen)
                .OrderByDescending(i => i.KayitTarihi)
                .Take(5)
                .Select(i => new {
                    i.Id,
                    i.FaturaNo,
                    FirmaGonderenAdi = i.FirmaGonderen != null ? i.FirmaGonderen.FirmaAdi : "",
                    i.ToplamTutar,
                    i.FaturaTarihi,
                    i.Durum
                })
                .ToListAsync();

            return Ok(recentInvoices);
        }

        [HttpGet("invoice-type-summary")]
        public async Task<IActionResult> GetInvoiceTypeSummary()
        {
            var summary = await _context.Invoices
                .Where(i => !string.IsNullOrEmpty(i.FaturaTuru))
                .GroupBy(i => i.FaturaTuru)
                .Select(g => new
                {
                    FaturaTuru = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            return Ok(summary);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using FisFaturaAPI.Data;
using FisFaturaAPI.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using ClosedXML.Excel;
using System.IO;
using System.Collections.Generic;
using System;

namespace FisFaturaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("preview")]
        public async Task<IActionResult> PreviewReport([FromBody] FisFaturaAPI.Models.ReportRequestDto request)
        {
            var query = _context.Invoices
                .Include(i => i.FirmaGonderen)
                .Include(i => i.FirmaAlici)
                .AsQueryable();

            if (request.StartDate.HasValue)
            {
                query = query.Where(i => i.FaturaTarihi >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(i => i.FaturaTarihi <= request.EndDate.Value);
            }

            var invoices = await query
                .OrderBy(i => i.FirmaGonderen.FirmaAdi)
                .ThenByDescending(i => i.FaturaTarihi)
                .Take(100) // Let's limit the preview to 100 records for performance
                .ToListAsync();

            var result = invoices.Select(invoice =>
            {
                var row = new Dictionary<string, object>();
                foreach (var col in request.Columns)
                {
                    row[col] = GetPropertyValue(invoice, col);
                }
                return row;
            }).ToList();

            return Ok(result);
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateReport([FromBody] FisFaturaAPI.Models.ReportRequestDto request)
        {
            var query = _context.Invoices
                .Include(i => i.FirmaGonderen)
                .Include(i => i.FirmaAlici)
                .AsQueryable();

            if (request.StartDate.HasValue)
            {
                query = query.Where(i => i.FaturaTarihi >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(i => i.FaturaTarihi <= request.EndDate.Value);
            }

            var invoices = await query
                .OrderBy(i => i.FirmaGonderen.FirmaAdi)
                .ThenByDescending(i => i.FaturaTarihi)
                .ToListAsync();

            // Create Excel file
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("FaturaRaporu");
                var currentRow = 1;

                // Create Header
                for (int i = 0; i < request.Columns.Count; i++)
                {
                    worksheet.Cell(currentRow, i + 1).Value = request.Columns[i];
                }

                // Create Rows
                foreach (var invoice in invoices)
                {
                    currentRow++;
                    for (int i = 0; i < request.Columns.Count; i++)
                    {
                        worksheet.Cell(currentRow, i + 1).Value = GetPropertyValue(invoice, request.Columns[i]);
                    }
                }

                // Format as a table
                if (currentRow > 1)
                {
                    var range = worksheet.Range(worksheet.Cell(1, 1), worksheet.Cell(currentRow, request.Columns.Count));
                    var table = range.CreateTable("FaturaRaporuTablosu");
                    table.Theme = XLTableTheme.TableStyleMedium2;
                    worksheet.Columns().AdjustToContents();
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "FaturaRaporu.xlsx");
                }
            }
        }
        
        private dynamic GetPropertyValue(Invoice invoice, string propertyName)
        {
            return propertyName switch
            {
                "FaturaNo" => invoice.FaturaNo ?? "",
                "FaturaTarihi" => invoice.FaturaTarihi?.ToString("dd.MM.yyyy") ?? "",
                "FirmaGonderen" => invoice.FirmaGonderen?.FirmaAdi ?? "",
                "FirmaAlici" => invoice.FirmaAlici?.FirmaAdi ?? "",
                "ToplamTutar" => (object)invoice.ToplamTutar ?? "",
                "KdvToplam" => (object)invoice.KdvToplam ?? "",
                "MatrahToplam" => (object)invoice.MatrahToplam ?? "",
                "GelirGider" => invoice.GelirGider ?? "",
                "FaturaTuru" => invoice.FaturaTuru ?? "",
                "Durum" => invoice.Durum ?? "",
                _ => ""
            };
        }
    }
} 
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using FisFaturaUI.Models;
using System.Text.Json;
using System.Globalization;

namespace FisFaturaUI.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DashboardController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            var viewModel = new DashboardViewModel();

            try
            {
                // Stats
                var statsResponse = await client.GetAsync("api/Dashboard/stats");
                if (statsResponse.IsSuccessStatusCode)
                {
                    var statsJson = await statsResponse.Content.ReadAsStringAsync();
                    viewModel.Stats = JsonConvert.DeserializeObject<DashboardStatsViewModel>(statsJson);
                }

                // Monthly Summary for Chart
                var monthlySummaryResponse = await client.GetAsync("api/Dashboard/monthly-summary");
                if (monthlySummaryResponse.IsSuccessStatusCode)
                {
                    var json = await monthlySummaryResponse.Content.ReadAsStringAsync();
                    viewModel.MonthlySummaryJson = json;
                }

                // Invoice Type Summary for Chart
                var invoiceTypeSummaryResponse = await client.GetAsync("api/Dashboard/invoice-type-summary");
                if (invoiceTypeSummaryResponse.IsSuccessStatusCode)
                {
                    var json = await invoiceTypeSummaryResponse.Content.ReadAsStringAsync();
                    viewModel.InvoiceTypeSummaryJson = json;
                }

                // Firms for Modal
                var firmsResponse = await client.GetAsync("api/Firm");
                if (firmsResponse.IsSuccessStatusCode)
                {
                    var json = await firmsResponse.Content.ReadAsStringAsync();
                    viewModel.Firms = JsonConvert.DeserializeObject<List<FirmViewModel>>(json) ?? new List<FirmViewModel>();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                ViewBag.ErrorMessage = "Dashboard verileri yüklenirken bir hata oluştu.";
            }
            
            return View(viewModel);
        }

        public async Task<IActionResult> Firmalarim()
        {
            var userId = _httpContextAccessor.HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            try
            {
                var client = _httpClientFactory.CreateClient("FisFaturaAPI");
                var response = await client.GetAsync("api/Firm");
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var firms = JsonConvert.DeserializeObject<List<FirmViewModel>>(jsonString);
                    ViewBag.Firmalar = firms;
                    return View(firms);
                }
                else
                {
                    ViewBag.ErrorMessage = "Firmalar çekilirken bir hata oluştu.";
                    return View(new List<FirmViewModel>());
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Hata: {ex.Message}";
                return View(new List<FirmViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFirmalar()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("FisFaturaAPI");
                var response = await client.GetAsync("api/Firm");
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var firms = JsonConvert.DeserializeObject<List<FirmViewModel>>(jsonString);
                    return PartialView("_FirmaSecModal", firms);
                }
                else
                {
                    return PartialView("_FirmaSecModal", new List<FirmViewModel>());
                }
            }
            catch (Exception ex)
            {
                return PartialView("_FirmaSecModal", new List<FirmViewModel>());
            }
        }
    }
} 
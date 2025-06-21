using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using FisFaturaUI.Models;
using System.Text;

namespace FisFaturaUI.Controllers
{
    public class FirmController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FirmController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public IActionResult Create()
        {
            var userId = _httpContextAccessor.HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["Message"] = "Kullanıcı oturumu bulunamadı.";
                return RedirectToAction("Login", "User");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateFirmViewModel model)
        {
            var userId = _httpContextAccessor.HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["Message"] = "Kullanıcı oturumu bulunamadı.";
                return RedirectToAction("Login", "User");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            var firmToCreate = new
            {
                model.FirmaAdi,
                model.VergiNo,
                EkleyenKullaniciId = userId.Value
            };

            var content = new StringContent(JsonConvert.SerializeObject(firmToCreate), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("api/Firm", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Firma başarıyla oluşturuldu.";
                return RedirectToAction("Firmalarim", "Dashboard");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", $"Firma oluşturulurken bir hata oluştu: {errorContent}");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _httpContextAccessor.HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["Message"] = "Kullanıcı oturumu bulunamadı.";
                return RedirectToAction("Login", "User");
            }

            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            var response = await client.GetAsync($"api/Firm/{id}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var firm = JsonConvert.DeserializeObject<dynamic>(content);
                
                var model = new EditFirmViewModel
                {
                    Id = firm.id,
                    FirmaAdi = firm.firmaAdi,
                    VergiNo = firm.vergiNo
                };
                
                return View(model);
            }

            TempData["Message"] = "Firma bulunamadı.";
            return RedirectToAction("Firmalarim", "Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditFirmViewModel model)
        {
            var userId = _httpContextAccessor.HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["Message"] = "Kullanıcı oturumu bulunamadı.";
                return RedirectToAction("Login", "User");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            var firmToUpdate = new
            {
                model.Id,
                model.FirmaAdi,
                model.VergiNo
            };

            var content = new StringContent(JsonConvert.SerializeObject(firmToUpdate), Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"api/Firm/{model.Id}", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Firma başarıyla güncellendi.";
                return RedirectToAction("Firmalarim", "Dashboard");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", $"Firma güncellenirken bir hata oluştu: {errorContent}");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _httpContextAccessor.HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Kullanıcı oturumu bulunamadı." });
            }

            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            var response = await client.DeleteAsync($"api/Firm/{id}");

            if (response.IsSuccessStatusCode)
            {
                return Json(new { success = true, message = "Firma başarıyla silindi." });
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return Json(new { success = false, message = $"Firma silinirken bir hata oluştu: {errorContent}" });
        }
    }
} 
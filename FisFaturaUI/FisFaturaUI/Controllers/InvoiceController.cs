using Microsoft.AspNetCore.Mvc;
using FisFaturaUI.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;
using System.Globalization;
using Newtonsoft.Json;

namespace FisFaturaUI.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<InvoiceController> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public InvoiceController(IHttpClientFactory httpClientFactory, ILogger<InvoiceController> logger, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(_configuration["ApiSettings:BaseUrl"]);
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? firmaId = null)
        {
            var firmaClient = _httpClientFactory.CreateClient("FisFaturaAPI");
            var firmaResponse = await firmaClient.GetAsync("api/Firm");
            if (firmaResponse.IsSuccessStatusCode)
            {
                var jsonString = await firmaResponse.Content.ReadAsStringAsync();
                var firms = Newtonsoft.Json.JsonConvert.DeserializeObject<List<FisFaturaUI.Models.FirmViewModel>>(jsonString);
                ViewBag.Firmalar = firms;
            }
            else
            {
                ViewBag.Firmalar = new List<FisFaturaUI.Models.FirmViewModel>();
            }
            var model = new CreateInvoicePageViewModel
            {
                OcrInvoice = new OcrInvoiceViewModel(),
                Senaryolar = new List<string> { "EARSIVFATURA", "TEMELFATURA" },
                OdemeYontemleri = new List<string> { "NAKIT", "KREDIKARTI", "HAVALE" },
                FaturaTurleri = new List<string> { 
                    "Temel Fatura", "Ticari Fatura", "Tevkifat", "Yolcu Beraberi Eşya", "İhracat Fatura", 
                    "Özel Fatura", "Hat Tipi Fatura", "Standart Kod Fatura", "Satış", 
                    "Özel Matrah", "İstisna", "İade" 
                },
                GelirGiderSecenekleri = new List<string> { "Gelir", "Gider" }
            };
            if (firmaId.HasValue)
            {
                var client = _httpClientFactory.CreateClient("FisFaturaAPI");
                var response = await client.GetAsync($"api/Firm/{firmaId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var firma = Newtonsoft.Json.JsonConvert.DeserializeObject<FirmViewModel>(json);
                    model.OcrInvoice.FirmaGonderenAdi = firma.FirmaAdi;
                    model.OcrInvoice.FirmaGonderenVergiNo = firma.VergiNo;
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessOcr(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "Dosya seçilmedi." });
                }

                using var content = new MultipartFormDataContent();
                using var fileContent = new StreamContent(file.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "file", file.FileName);

                var response = await _httpClient.PostAsync("/api/Invoice/process-ocr", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Python OCR JSON: " + result);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = null,
                        PropertyNameCaseInsensitive = true
                    };
                    var ocrResult = System.Text.Json.JsonSerializer.Deserialize<OcrResult>(result, options);
                    _logger.LogInformation($"C# OCR Result: FaturaNo={ocrResult?.FaturaNo}, FirmaAliciAdi={ocrResult?.FirmaAliciAdi}, ToplamTutar={ocrResult?.ToplamTutar}, Kdv20={ocrResult?.Kdv20}");
                    return new JsonResult(new { success = true, data = ocrResult }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                }
                else
                {
                    return Json(new { success = false, message = "OCR işlemi başarısız oldu." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessReceiptOcr(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "Dosya seçilmedi." });
                }

                using var content = new MultipartFormDataContent();
                using var fileContent = new StreamContent(file.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "file", file.FileName);

                // Forward the request to the FisFaturaAPI
                var response = await _httpClient.PostAsync("api/Invoice/process-receipt-ocr", content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    // The result from the API is already in the desired format { success: true, data: { ... } }
                    // So, we can return it directly as a ContentResult.
                    return Content(result, "application/json");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Fiş OCR API çağrısı başarısız: {errorContent}");
                    return Json(new { success = false, message = "Fiş OCR işlemi API'de başarısız oldu." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fiş OCR işlemi sırasında beklenmedik bir hata oluştu.");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(Invoice invoice)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(invoice);
                }

                var json = System.Text.Json.JsonSerializer.Serialize(invoice);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/Invoice", content);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Message"] = "Fatura başarıyla oluşturuldu.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Fatura oluşturulurken bir hata oluştu.");
                    return View(invoice);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(invoice);
            }
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(OcrInvoiceViewModel model)
        {
            if (model.UploadedFile == null || model.UploadedFile.Length == 0)
            {
                ModelState.AddModelError("", "Lütfen bir dosya seçin.");
                return View();
            }

            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            using var content = new MultipartFormDataContent();
            using var fileStream = model.UploadedFile.OpenReadStream();
            content.Add(new StreamContent(fileStream), "file", model.UploadedFile.FileName);

            var response = await client.PostAsync("api/invoice/upload-ocr", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var invoice = System.Text.Json.JsonSerializer.Deserialize<object>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return View("Result", invoice);
            }

            ModelState.AddModelError("", "OCR başarısız.");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Save(CreateInvoicePageViewModel model, IFormFile? faturaDosyasi)
        {
            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            
            try
            {
                var invoice = await MapToInvoice(model.OcrInvoice, client);

                if (faturaDosyasi != null && faturaDosyasi.Length > 0)
                    {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder))
                            {
                        Directory.CreateDirectory(uploadsFolder);
                }
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + faturaDosyasi.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await faturaDosyasi.CopyToAsync(fileStream);
                    }
                    invoice.FaturaResimYolu = "/uploads/" + uniqueFileName;
                }

            _logger.LogInformation($"API'ye gönderilen JSON: {System.Text.Json.JsonSerializer.Serialize(invoice)}");

            var json = System.Text.Json.JsonSerializer.Serialize(invoice);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("api/Invoice", content);

            if (response.IsSuccessStatusCode)
            {
                return Json(new { success = true, message = "Fatura başarıyla kaydedildi." });
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Fatura oluşturulurken hata oluştu. Status: {response.StatusCode}, Content: {error}");
                return Json(new { success = false, message = $"Fatura kaydedilirken bir hata oluştu: {error}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatura kaydedilirken beklenmedik bir hata oluştu.");
                return Json(new { success = false, message = $"Fatura kaydedilirken bir sunucu hatası oluştu: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            var response = await client.GetAsync("api/Invoice");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                var invoices = System.Text.Json.JsonSerializer.Deserialize<List<InvoiceListViewModel>>(jsonString, options);
                return View(invoices);
            }

            return View(new List<InvoiceListViewModel>());
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            var response = await client.DeleteAsync($"api/Invoice/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Fatura başarıyla silindi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Fatura silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(List));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            var response = await client.GetAsync($"api/Invoice/{id}");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("API'den gelen fatura detayı JSON: {jsonString}", jsonString);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var invoice = System.Text.Json.JsonSerializer.Deserialize<Invoice>(jsonString, options);

                var firmaResponse = await client.GetAsync("api/Firm");
                List<FirmViewModel> firms = new List<FirmViewModel>();
                if (firmaResponse.IsSuccessStatusCode)
                {
                    var firmaJsonString = await firmaResponse.Content.ReadAsStringAsync();
                    firms = Newtonsoft.Json.JsonConvert.DeserializeObject<List<FirmViewModel>>(firmaJsonString);
                    ViewBag.Firmalar = firms;
                }

                var model = new CreateInvoicePageViewModel
                {
                    Invoice = invoice,
                    Senaryolar = new List<string> { "EARSIVFATURA", "TEMELFATURA" },
                    OdemeYontemleri = new List<string> { "NAKIT", "KREDIKARTI", "HAVALE" },
                    FaturaTurleri = new List<string> {
                        "Temel Fatura", "Ticari Fatura", "Tevkifat", "Yolcu Beraberi Eşya", "İhracat Fatura",
                        "Özel Fatura", "Hat Tipi Fatura", "Standart Kod Fatura", "Satış",
                        "Özel Matrah", "İstisna", "İade"
                    },
                    GelirGiderSecenekleri = new List<string> { "Gelir", "Gider" }
                };

                if (invoice != null)
                {
                    var gonderenFirma = firms.FirstOrDefault(f => f.Id == invoice.FirmaGonderenId);
                    var aliciFirma = firms.FirstOrDefault(f => f.Id == invoice.FirmaAliciId);

                    model.OcrInvoice = new OcrInvoiceViewModel
                    {
                        Id = invoice.Id,
                        FaturaNo = invoice.FaturaNo,
                        FaturaTarihi = invoice.FaturaTarihi?.ToString("yyyy-MM-dd"),
                        ToplamTutar = invoice.ToplamTutar?.ToString("F2", CultureInfo.InvariantCulture),
                        KdvToplamTutar = invoice.KdvToplam?.ToString("F2", CultureInfo.InvariantCulture),
                        FirmaGonderenAdi = gonderenFirma?.FirmaAdi,
                        FirmaGonderenVergiNo = gonderenFirma?.VergiNo,
                        FirmaAliciAdi = aliciFirma?.FirmaAdi,
                        FirmaAliciVergiNo = aliciFirma?.VergiNo,
                        Senaryo = invoice.Senaryo,
                        OdemeYontemi = invoice.OdemeTuru,
                        GelirGider = invoice.GelirGider,
                        FaturaTipi = invoice.FaturaTuru,
                        Kdv0 = invoice.Kdv_0?.ToString("F2", CultureInfo.InvariantCulture),
                        Matrah0 = invoice.Matrah_0?.ToString("F2", CultureInfo.InvariantCulture),
                        Kdv1 = invoice.Kdv_1?.ToString("F2", CultureInfo.InvariantCulture),
                        Matrah1 = invoice.Matrah_1?.ToString("F2", CultureInfo.InvariantCulture),
                        Kdv8 = invoice.Kdv_8?.ToString("F2", CultureInfo.InvariantCulture),
                        Matrah8 = invoice.Matrah_8?.ToString("F2", CultureInfo.InvariantCulture),
                        Kdv10 = invoice.Kdv_10?.ToString("F2", CultureInfo.InvariantCulture),
                        Matrah10 = invoice.Matrah_10?.ToString("F2", CultureInfo.InvariantCulture),
                        Kdv18 = invoice.Kdv_18?.ToString("F2", CultureInfo.InvariantCulture),
                        Matrah18 = invoice.Matrah_18?.ToString("F2", CultureInfo.InvariantCulture),
                        Kdv20 = invoice.Kdv_20?.ToString("F2", CultureInfo.InvariantCulture),
                        Matrah20 = invoice.Matrah_20?.ToString("F2", CultureInfo.InvariantCulture),
                        FaturaResimYolu = invoice.FaturaResimYolu
                    };
                }

                return View("Edit", model);
            }

            TempData["ErrorMessage"] = "Fatura bulunamadı.";
            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, CreateInvoicePageViewModel model, IFormFile? faturaDosyasi)
        {
            var client = _httpClientFactory.CreateClient("FisFaturaAPI");

            // Önemli: Gelen model CreateInvoicePageViewModel, API ise Invoice bekliyor.
            // Bu yüzden gelen OcrInvoiceViewModel'i bir Invoice nesnesine çevirmeliyiz.
            var invoiceToUpdate = await MapToInvoice(model.OcrInvoice, client);
            invoiceToUpdate.Id = id; // Güncellenecek faturanın ID'si.

            if (faturaDosyasi != null && faturaDosyasi.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + faturaDosyasi.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await faturaDosyasi.CopyToAsync(fileStream);
                }
                invoiceToUpdate.FaturaResimYolu = "/uploads/" + uniqueFileName;
            }
            else
            {
                // Yeni resim yüklenmediyse, eski resim yolunu koru.
                invoiceToUpdate.FaturaResimYolu = model.OcrInvoice.FaturaResimYolu;
            }

            var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(invoiceToUpdate), Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"api/Invoice/{id}", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                return Json(new { success = true, message = "Fatura başarıyla güncellendi." });
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError($"Fatura güncellenirken hata oluştu. Status: {response.StatusCode}, Content: {error}");
            return Json(new { success = false, message = $"Fatura güncellenirken bir hata oluştu: {error}" });
        }

        private async Task<Invoice> MapToInvoice(OcrInvoiceViewModel ocrModel, HttpClient client)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                // Bu durumun bir şekilde ele alınması lazım, belki bir exception fırlatılabilir.
                // Şimdilik varsayılan bir kullanıcı ID'si veya hata yönetimi yapılabilir.
                throw new InvalidOperationException("User session not found.");
            }

            // Firma ID'lerini ad ve vergi no'dan bul
            var firmaGonderenId = await GetOrCreateFirmId(ocrModel.FirmaGonderenAdi, ocrModel.FirmaGonderenVergiNo, userId.Value, client);
            var firmaAliciId = await GetOrCreateFirmId(ocrModel.FirmaAliciAdi, ocrModel.FirmaAliciVergiNo, userId.Value, client);
            
            return new Invoice
            {
                Id = ocrModel.Id,
                FaturaNo = ocrModel.FaturaNo,
                FaturaTarihi = DateTime.TryParse(ocrModel.FaturaTarihi, out var faturaTarihi) ? faturaTarihi : null,
                ToplamTutar = decimal.TryParse(ocrModel.ToplamTutar?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var toplamTutar) ? toplamTutar : null,
                OdemeTuru = ocrModel.OdemeYontemi,
                IcerikTuru = ocrModel.IcerikTuru ?? "Standart",
                FaturaTuru = ocrModel.FaturaTipi,
                Senaryo = ocrModel.Senaryo,
                GelirGider = ocrModel.GelirGider,
                KdvToplam = decimal.TryParse(ocrModel.KdvToplamTutar?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var kdvToplam) ? kdvToplam : null,
                MatrahToplam = decimal.TryParse(ocrModel.MatrahToplamTutar?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var matrahToplam) ? matrahToplam : null,
                Kdv_0 = decimal.TryParse(ocrModel.Kdv0?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var kdv0) ? kdv0 : null,
                Matrah_0 = decimal.TryParse(ocrModel.Matrah0?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var matrah0) ? matrah0 : null,
                Kdv_1 = decimal.TryParse(ocrModel.Kdv1?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var kdv1) ? kdv1 : null,
                Matrah_1 = decimal.TryParse(ocrModel.Matrah1?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var matrah1) ? matrah1 : null,
                Kdv_8 = decimal.TryParse(ocrModel.Kdv8?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var kdv8) ? kdv8 : null,
                Matrah_8 = decimal.TryParse(ocrModel.Matrah8?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var matrah8) ? matrah8 : null,
                Kdv_10 = decimal.TryParse(ocrModel.Kdv10?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var kdv10) ? kdv10 : null,
                Matrah_10 = decimal.TryParse(ocrModel.Matrah10?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var matrah10) ? matrah10 : null,
                Kdv_18 = decimal.TryParse(ocrModel.Kdv18?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var kdv18) ? kdv18 : null,
                Matrah_18 = decimal.TryParse(ocrModel.Matrah18?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var matrah18) ? matrah18 : null,
                Kdv_20 = decimal.TryParse(ocrModel.Kdv20?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var kdv20) ? kdv20 : null,
                Matrah_20 = decimal.TryParse(ocrModel.Matrah20?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var matrah20) ? matrah20 : null,
                KayitTarihi = DateTime.Now,
                FirmaGonderenId = firmaGonderenId,
                FirmaAliciId = firmaAliciId,
                KaydedenKullaniciId = userId.Value,
                FaturaResimYolu = ocrModel.FaturaResimYolu
            };
        }

        private async Task<int?> GetOrCreateFirmId(string firmaAdi, string vergiNo, int userId, HttpClient client)
        {
            if (string.IsNullOrWhiteSpace(firmaAdi) || string.IsNullOrWhiteSpace(vergiNo))
            {
                return null;
            }

            var firmsResponse = await client.GetAsync("api/Firm");
            if (firmsResponse.IsSuccessStatusCode)
            {
                var firmsJson = await firmsResponse.Content.ReadAsStringAsync();
                var existingFirms = System.Text.Json.JsonSerializer.Deserialize<List<FirmViewModel>>(firmsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var existingFirm = existingFirms.FirstOrDefault(f =>
                    f.FirmaAdi.Equals(firmaAdi, StringComparison.OrdinalIgnoreCase) &&
                    f.VergiNo.Equals(vergiNo, StringComparison.OrdinalIgnoreCase));

                if (existingFirm != null)
                {
                    return existingFirm.Id;
                }
            }

            // Create new firm
            var newFirmDto = new { FirmaAdi = firmaAdi, VergiNo = vergiNo, EkleyenKullaniciId = userId };
            var newFirmContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(newFirmDto), Encoding.UTF8, "application/json");
            var createFirmResponse = await client.PostAsync("api/Firm", newFirmContent);

            if (createFirmResponse.IsSuccessStatusCode)
            {
                var newFirmJson = await createFirmResponse.Content.ReadAsStringAsync();
                var createdFirm = System.Text.Json.JsonSerializer.Deserialize<FirmViewModel>(newFirmJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return createdFirm?.Id;
            }

            return null;
        }

        private async Task<List<FirmViewModel>> GetFirms()
        {
            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            var response = await client.GetAsync("api/Firm");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<FirmViewModel>>(json) ?? new List<FirmViewModel>();
            }
            return new List<FirmViewModel>();
        }

        [HttpGet]
        public async Task<IActionResult> ReceiptUpload(string firmaAdi, int? firmaId = null)
        {
            ViewBag.Firmalar = await GetFirms();

            var model = new ReceiptUploadViewModel
            {
                FirmaId = firmaId,
                FirmaAdi = firmaAdi,
                IcerikTurleri = new List<string> { "Yiyecek", "İçecek", "Yemek", "Kırtasiye", "Market", "Yiyecek-İçecek", "Akaryakıt", "Tekstil", "İlaç", "Sağlık", "Diğer" },
                OdemeSekilleri = new List<string> { "KREDİ", "NAKİT", "ÇEK" },
                GelirGiderSecenekleri = new List<string> { "Gelir", "Gider" }
            };

            if (firmaId.HasValue)
            {
                var client = _httpClientFactory.CreateClient("FisFaturaAPI");
                var response = await client.GetAsync($"api/Firm/{firmaId.Value}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var firma = JsonConvert.DeserializeObject<FirmViewModel>(json);
                    if (firma != null) {
                        model.FirmaAdi = firma.FirmaAdi;
                        model.VergiNo = firma.VergiNo;
                    }
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ReceiptUpload(ReceiptUploadViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı oturumu bulunamadı. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "User");
            }

            if (!ModelState.IsValid)
            {
                // Repopulate dropdowns if model validation fails
                model.IcerikTurleri = new List<string> { "Yiyecek", "İçecek", "Yemek", "Kırtasiye", "Market", "Yiyecek-İçecek", "Akaryakıt", "Tekstil", "İlaç", "Sağlık", "Diğer" };
                model.OdemeSekilleri = new List<string> { "KREDİ", "NAKİT", "ÇEK" };
                model.GelirGiderSecenekleri = new List<string> { "Gelir", "Gider" };
                return View(model);
            }

            string? savedFilePath = null;
            if (model.Dosya != null && model.Dosya.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = $"{Guid.NewGuid()}_{model.Dosya.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Dosya.CopyToAsync(stream);
                }
                savedFilePath = $"/uploads/{uniqueFileName}";
            }

            // Receipt modeline uygun nesne oluştur
            var receiptToSave = new {
                FirmaAdi = model.FirmaAdi,
                VergiNo = model.VergiNo,
                FisNo = model.FisNo,
                Tarih = DateTime.TryParse(model.Tarih, out var tarih) ? (DateTime?)tarih : null,
                ToplamTutar = decimal.TryParse(model.ToplamTutar?.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var toplamTutar) ? (decimal?)toplamTutar : null,
                IcerikTuru = model.IcerikTuru,
                OdemeSekli = model.OdemeSekli,
                GelirGider = model.GelirGider,
                KdvOranlariJson = System.Text.Json.JsonSerializer.Serialize(model.KdvOranlari),
                MatrahOranlariJson = System.Text.Json.JsonSerializer.Serialize(model.MatrahOranlari),
                FisResimYolu = savedFilePath,
                KayitTarihi = DateTime.Now,
                KullaniciId = userId
            };

            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            var json = System.Text.Json.JsonSerializer.Serialize(receiptToSave);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/Receipt", content);
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Fiş başarıyla kaydedildi.";
                return RedirectToAction(nameof(ReceiptList));
            }
            else
            {
                ModelState.AddModelError("", "Fiş kaydedilirken bir hata oluştu.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ReceiptList()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı oturumu bulunamadı.";
                return RedirectToAction("Index", "Dashboard");
            }
            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            var response = await client.GetAsync($"/api/Receipt?userId={userId}");
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var receipts = System.Text.Json.JsonSerializer.Deserialize<List<FisFaturaUI.Models.ReceiptListViewModel>>(jsonString, options);
                return View(receipts);
            }
            return View(new List<FisFaturaUI.Models.ReceiptListViewModel>());
        }

        [HttpGet]
        public async Task<IActionResult> EditReceipt(int id)
        {
            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            var response = await client.GetAsync($"/api/Receipt/{id}");
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var receipt = System.Text.Json.JsonSerializer.Deserialize<FisFaturaUI.Models.ReceiptListViewModel>(jsonString, options);
                return View(receipt);
            }
            TempData["ErrorMessage"] = "Fiş bulunamadı.";
            return RedirectToAction(nameof(ReceiptList));
        }

        [HttpPost]
        public async Task<IActionResult> EditReceipt(FisFaturaUI.Models.ReceiptListViewModel model)
        {
            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            var json = System.Text.Json.JsonSerializer.Serialize(model);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"/api/Receipt/{model.Id}", content);
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Fiş başarıyla güncellendi.";
                return RedirectToAction(nameof(ReceiptList));
            }
            TempData["ErrorMessage"] = "Fiş güncellenirken bir hata oluştu.";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteReceipt(int id)
        {
            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            var response = await client.DeleteAsync($"/api/Receipt/{id}");
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Fiş başarıyla silindi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Fiş silinirken bir hata oluştu.";
            }
            return RedirectToAction(nameof(ReceiptList));
        }
    }

    public class OcrResult
    {
        public string FirmaAliciAdi { get; set; }
        public string FirmaAliciVergiNo { get; set; }
        public string FaturaNo { get; set; }
        public string FaturaTarihi { get; set; }
        public string ToplamTutar { get; set; }
        public string Kdv0 { get; set; }
        public string Kdv1 { get; set; }
        public string Kdv8 { get; set; }
        public string Kdv10 { get; set; }
        public string Kdv18 { get; set; }
        public string Kdv20 { get; set; }
        public string Matrah0 { get; set; }
        public string Matrah1 { get; set; }
        public string Matrah8 { get; set; }
        public string Matrah10 { get; set; }
        public string Matrah18 { get; set; }
        public string Matrah20 { get; set; }
        public string KdvToplamTutar { get; set; }
        public string MatrahToplamTutar { get; set; }
        public string IcerikTuru { get; set; }
        public string OdemeTuru { get; set; }
        public List<string> RawOcrLines { get; set; }
    }
}

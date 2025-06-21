using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FisFaturaAPI.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FisFaturaAPI.Services
{
    public class OcrService : IOcrService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OcrService> _logger;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public OcrService(HttpClient httpClient, IConfiguration configuration, ILogger<OcrService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.BaseAddress = new Uri(configuration["OcrService:BaseUrl"]);
            _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<OcrResult> ProcessInvoiceAsync(IFormFile file)
        {
            _logger.LogInformation("Fatura OCR işlemi için istek gönderiliyor.");
            return await ProcessOcrRequestAsync("/ocr", file);
        }

        public async Task<OcrResult> ProcessReceiptAsync(IFormFile file)
        {
            _logger.LogInformation("Fiş OCR işlemi için istek gönderiliyor.");
            return await ProcessOcrRequestAsync("/receipt", file);
        }

        private async Task<OcrResult> ProcessOcrRequestAsync(string endpoint, IFormFile file)
        {
            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.FileName);

            var response = await _httpClient.PostAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"OCR servisinden başarılı yanıt alındı. Endpoint: {endpoint}");
                return JsonSerializer.Deserialize<OcrResult>(jsonResponse, _jsonSerializerOptions);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"OCR servisi hatası. Endpoint: {endpoint}, Status: {response.StatusCode}, Content: {errorContent}");
                throw new Exception($"OCR service at {endpoint} failed: {errorContent}");
            }
        }
    }

    public class OcrResult
    {
        public string VergiNo { get; set; }
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

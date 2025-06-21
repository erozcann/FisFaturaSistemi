using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FisFaturaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FaturaController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public FaturaController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("ocr")]
        public async Task<IActionResult> GorseldenOcrOku([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Dosya gönderilmedi.");

            var filePath = Path.GetTempFileName();
            using (var stream = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            var client = _httpClientFactory.CreateClient();
            var content = new MultipartFormDataContent();
            content.Add(new StreamContent(System.IO.File.OpenRead(filePath)), "file", file.FileName);

            var response = await client.PostAsync("http://127.0.0.1:5000/ocr", content);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "OCR servisi hata verdi.");

            var json = await response.Content.ReadAsStringAsync();
            return Ok(json);
        }
    }

}

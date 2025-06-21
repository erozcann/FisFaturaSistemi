using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FisFaturaAPI.Data;
using FisFaturaAPI.Models;
using FisFaturaAPI.Services;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace FisFaturaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOcrService _ocrService;
        private readonly ILogger<InvoiceController> _logger;
        private readonly HttpClient _httpClient;

        public InvoiceController(ApplicationDbContext context, IOcrService ocrService, ILogger<InvoiceController> logger, HttpClient httpClient)
        {
            _context = context;
            _ocrService = ocrService;
            _logger = logger;
            _httpClient = httpClient;
        }

        // ✅ POST: api/invoice → Yeni fatura kaydı
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Invoice invoice)
        {
            try
            {
                // Eğer KayitTarihi boş ise şu anki zamanı ata
                if (invoice.KayitTarihi == null)
                {
                    invoice.KayitTarihi = DateTime.Now;
                }

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatura oluşturulurken hata oluştu");
                return StatusCode(500, "Fatura oluşturulurken bir hata oluştu.");
            }
        }

        // ✅ GET: api/invoice → Tüm faturaları listele (firma bilgileri dahil)
        [HttpGet]
        public IActionResult GetAllInvoices()
        {
            var invoices = _context.Invoices
                .Include(i => i.FirmaGonderen)
                .Include(i => i.FirmaAlici)
                .Select(i => new {
                    i.Id,
                    i.FaturaNo,
                    i.FaturaTarihi,
                    FirmaGonderenAdi = i.FirmaGonderen != null ? i.FirmaGonderen.FirmaAdi : "",
                    FirmaAliciAdi = i.FirmaAlici != null ? i.FirmaAlici.FirmaAdi : "",
                    i.ToplamTutar,
                    i.OdemeTuru,
                    i.IcerikTuru,
                    i.FaturaTuru,
                    i.GelirGider,
                    i.KayitTarihi,
                    FaturaResimYolu = i.FaturaResimYolu
                })
                .ToList();
            return Ok(invoices);
        }

        [HttpPost("upload-ocr")]
        public async Task<IActionResult> UploadOcrInvoice(IFormFile file)
        {
            try
            {
                var ocrResult = await _ocrService.ProcessInvoiceAsync(file);
                if (ocrResult == null)
                    return BadRequest("OCR servisi başarısız.");

                var firma = _context.Firms.FirstOrDefault(f => f.VergiNo == ocrResult.VergiNo);
                if (firma == null)
                    return BadRequest("Firma veritabanında bulunamadı.");

                var invoice = new Invoice
                {
                    FirmaGonderenId = firma.Id,
                    FirmaAliciId = firma.Id,
                    FaturaNo = ocrResult.FaturaNo,
                    FaturaTarihi = DateTime.Parse(ocrResult.FaturaTarihi),
                    ToplamTutar = decimal.Parse(ocrResult.ToplamTutar),
                    KayitTarihi = DateTime.Now,
                    KaydedenKullaniciId = 1,
                    GelirGider = "Gelir",
                    FaturaTuru = "E-Fatura"
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                // Sadece temel alanları dön
                return Ok(new {
                    invoice.Id,
                    invoice.FaturaNo,
                    invoice.FaturaTarihi,
                    invoice.FaturaTuru,
                    invoice.FirmaAliciId,
                    invoice.FirmaGonderenId,
                    invoice.GelirGider,
                    invoice.IcerikTuru,
                    invoice.KaydedenKullaniciId,
                    invoice.KayitTarihi,
                    invoice.KdvToplam,
                    invoice.Kdv_0,
                    invoice.Kdv_1,
                    invoice.Kdv_10,
                    invoice.Kdv_18,
                    invoice.Kdv_20,
                    invoice.Kdv_8,
                    invoice.MatrahToplam,
                    invoice.Matrah_0,
                    invoice.Matrah_1,
                    invoice.Matrah_10,
                    invoice.Matrah_18,
                    invoice.Matrah_20,
                    invoice.Matrah_8,
                    invoice.OdemeTuru,
                    invoice.Senaryo,
                    invoice.ToplamTutar
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR işlemi sırasında hata oluştu");
                return StatusCode(500, "OCR işlemi sırasında bir hata oluştu.");
            }
        }

        [HttpPost("process-ocr")]
        public async Task<IActionResult> ProcessOcr(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Dosya yüklenmedi.");
            }

            try
            {
                var ocrResult = await _ocrService.ProcessInvoiceAsync(file);
                return Ok(ocrResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR işlemi sırasında bir hata oluştu.");
                return StatusCode(500, "OCR işlemi sırasında bir hata oluştu.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var invoice = await _context.Invoices
                .Where(i => i.Id == id)
                .Select(i => new {
                    i.Id,
                    i.FaturaNo,
                    i.FaturaTarihi,
                    i.FaturaTuru,
                    i.FirmaAliciId,
                    i.FirmaGonderenId,
                    i.GelirGider,
                    i.IcerikTuru,
                    i.KaydedenKullaniciId,
                    i.KayitTarihi,
                    i.KdvToplam,
                    i.Kdv_0,
                    i.Kdv_1,
                    i.Kdv_10,
                    i.Kdv_18,
                    i.Kdv_20,
                    i.Kdv_8,
                    i.MatrahToplam,
                    i.Matrah_0,
                    i.Matrah_1,
                    i.Matrah_10,
                    i.Matrah_18,
                    i.Matrah_20,
                    i.Matrah_8,
                    i.OdemeTuru,
                    i.Senaryo,
                    i.ToplamTutar,
                    FaturaResimYolu = i.FaturaResimYolu
                })
                .FirstOrDefaultAsync();
            if (invoice == null)
            {
                return NotFound();
            }
            return Ok(invoice);
        }

        // ✅ DELETE: api/invoice/{id} → Fatura silme
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            // İsteğe bağlı: Faturayla ilişkili dosyayı da sil
            // if (!string.IsNullOrEmpty(invoice.FaturaResimYolu))
            // {
            //     var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", invoice.FaturaResimYolu.TrimStart('/'));
            //     if (System.IO.File.Exists(filePath))
            //     {
            //         System.IO.File.Delete(filePath);
            //     }
            // }

            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ✅ PUT: api/invoice/{id} → Fatura güncelleme
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Invoice invoice)
        {
            if (id != invoice.Id)
            {
                return BadRequest("ID uyuşmazlığı.");
            }

            var existingInvoice = await _context.Invoices.FindAsync(id);
            if (existingInvoice == null)
            {
                return NotFound("Güncellenecek fatura bulunamadı.");
            }

            // Gelen verileri mevcut fatura üzerine aktar
            existingInvoice.FaturaNo = invoice.FaturaNo;
            existingInvoice.FaturaTarihi = invoice.FaturaTarihi;
            existingInvoice.FaturaTuru = invoice.FaturaTuru;
            existingInvoice.Senaryo = invoice.Senaryo;
            existingInvoice.GelirGider = invoice.GelirGider;
            existingInvoice.OdemeTuru = invoice.OdemeTuru;
            existingInvoice.IcerikTuru = invoice.IcerikTuru;
            existingInvoice.ToplamTutar = invoice.ToplamTutar;
            existingInvoice.KdvToplam = invoice.KdvToplam;
            existingInvoice.MatrahToplam = invoice.MatrahToplam;
            existingInvoice.Kdv_0 = invoice.Kdv_0;
            existingInvoice.Matrah_0 = invoice.Matrah_0;
            existingInvoice.Kdv_1 = invoice.Kdv_1;
            existingInvoice.Matrah_1 = invoice.Matrah_1;
            existingInvoice.Kdv_8 = invoice.Kdv_8;
            existingInvoice.Matrah_8 = invoice.Matrah_8;
            existingInvoice.Kdv_10 = invoice.Kdv_10;
            existingInvoice.Matrah_10 = invoice.Matrah_10;
            existingInvoice.Kdv_18 = invoice.Kdv_18;
            existingInvoice.Matrah_18 = invoice.Matrah_18;
            existingInvoice.Kdv_20 = invoice.Kdv_20;
            existingInvoice.Matrah_20 = invoice.Matrah_20;
            existingInvoice.FirmaGonderenId = invoice.FirmaGonderenId;
            existingInvoice.FirmaAliciId = invoice.FirmaAliciId;

            // Fatura resim yolunu sadece yeni bir yol belirtilmişse güncelle
            if (!string.IsNullOrEmpty(invoice.FaturaResimYolu))
            {
                existingInvoice.FaturaResimYolu = invoice.FaturaResimYolu;
            }

            _context.Entry(existingInvoice).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Invoices.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost("process-receipt-ocr")]
        public async Task<IActionResult> ProcessReceiptOcr(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Dosya yüklenmedi.");
            }

            try
            {
                var ocrResult = await _ocrService.ProcessReceiptAsync(file);
                return Ok(new { success = true, data = ocrResult });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fiş OCR işlemi sırasında bir hata oluştu.");
                return StatusCode(500, "Fiş OCR işlemi sırasında bir sunucu hatası oluştu.");
            }
        }

        [HttpGet("/api/Receipt/{id}")]
        public IActionResult GetReceiptById(int id)
        {
            var receipt = _context.Receipts.FirstOrDefault(r => r.Id == id);
            if (receipt == null)
                return NotFound();
            return Ok(receipt);
        }

        [HttpPut("/api/Receipt/{id}")]
        public async Task<IActionResult> UpdateReceipt(int id, [FromBody] Receipt model)
        {
            var receipt = _context.Receipts.FirstOrDefault(r => r.Id == id);
            if (receipt == null)
                return NotFound();
            // Alanları güncelle
            receipt.FirmaAdi = model.FirmaAdi;
            receipt.VergiNo = model.VergiNo;
            receipt.FisNo = model.FisNo;
            receipt.Tarih = model.Tarih;
            receipt.ToplamTutar = model.ToplamTutar;
            receipt.IcerikTuru = model.IcerikTuru;
            receipt.OdemeSekli = model.OdemeSekli;
            receipt.GelirGider = model.GelirGider;
            receipt.KdvOranlariJson = model.KdvOranlariJson;
            receipt.MatrahOranlariJson = model.MatrahOranlariJson;
            receipt.FisResimYolu = model.FisResimYolu;
            await _context.SaveChangesAsync();
            return Ok(receipt);
        }

        [HttpDelete("/api/Receipt/{id}")]
        public async Task<IActionResult> DeleteReceipt(int id)
        {
            var receipt = _context.Receipts.FirstOrDefault(r => r.Id == id);
            if (receipt == null)
                return NotFound();
            _context.Receipts.Remove(receipt);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ReceiptController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReceiptController> _logger;

        public ReceiptController(ApplicationDbContext context, ILogger<ReceiptController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        [Route("/api/Receipt")] // POST: api/Receipt
        public async Task<IActionResult> CreateReceipt([FromBody] Receipt receipt)
        {
            if (receipt == null)
                return BadRequest("Geçersiz veri.");
            receipt.KayitTarihi = DateTime.Now;
            _context.Receipts.Add(receipt);
            await _context.SaveChangesAsync();
            return Ok(receipt);
        }

        [HttpGet]
        [Route("/api/Receipt")]
        public IActionResult GetReceipts([FromQuery] int? userId)
        {
            var query = _context.Receipts.AsQueryable();
            if (userId.HasValue)
            {
                query = query.Where(r => r.KullaniciId == userId);
            }
            var receipts = query.OrderByDescending(r => r.KayitTarihi).ToList();
            return Ok(receipts);
        }
    }
}

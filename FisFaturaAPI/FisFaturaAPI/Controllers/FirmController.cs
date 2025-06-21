using Microsoft.AspNetCore.Mvc;
using FisFaturaAPI.Data;
using FisFaturaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FisFaturaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FirmController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FirmController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ POST: api/firm
        [HttpPost]
        public IActionResult CreateFirm([FromBody] CreateFirmDto dto)
        {
            var firm = new Firm
            {
                FirmaAdi = dto.FirmaAdi,
                VergiNo = dto.VergiNo,
                EkleyenKullaniciId = dto.EkleyenKullaniciId,
                KayitTarihi = DateTime.Now
            };

            _context.Firms.Add(firm);
            _context.SaveChanges();

            return Ok(firm);
        }

        // ✅ GET: api/firm
        [HttpGet]
        public IActionResult GetAllFirms()
        {
            var firms = _context.Firms.ToList();
            return Ok(firms);
        }

        // ✅ GET: api/firm/{id}
        [HttpGet("{id}")]
        public IActionResult GetFirmById(int id)
        {
            var firm = _context.Firms.FirstOrDefault(f => f.Id == id);
            if (firm == null)
                return NotFound();

            return Ok(firm);
        }

        // ✅ PUT: api/firm/{id}
        [HttpPut("{id}")]
        public IActionResult UpdateFirm(int id, [FromBody] UpdateFirmDto dto)
        {
            var firm = _context.Firms.FirstOrDefault(f => f.Id == id);
            if (firm == null)
                return NotFound();

            firm.FirmaAdi = dto.FirmaAdi;
            firm.VergiNo = dto.VergiNo;

            _context.SaveChanges();
            return Ok(firm);
        }

        // ✅ DELETE: api/firm/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteFirm(int id)
        {
            var firm = _context.Firms.FirstOrDefault(f => f.Id == id);
            if (firm == null)
                return NotFound();

            // Firma ile ilişkili faturalar var mı kontrol et
            var hasInvoices = _context.Invoices.Any(i => i.FirmaAliciId == id || i.FirmaGonderenId == id);
            if (hasInvoices)
                return BadRequest("Bu firmaya ait faturalar bulunduğu için silinemez.");

            _context.Firms.Remove(firm);
            _context.SaveChanges();

            return Ok(new { message = "Firma başarıyla silindi." });
        }

        // ✅ GET: api/firm/user/{userId}
        [HttpGet("user/{userId}")]
        public IActionResult GetFirmsByUserId(int userId)
        {
            var firms = _context.Firms.Where(f => f.EkleyenKullaniciId == userId).ToList();
            return Ok(firms);
        }
    }
}

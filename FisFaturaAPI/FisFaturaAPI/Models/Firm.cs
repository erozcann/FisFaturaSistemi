using System.Collections.Generic;
using FisFaturaAPI.Models;

namespace FisFaturaAPI.Models
{
    public class Firm
    {
        public int Id { get; set; }
        public string FirmaAdi { get; set; } = string.Empty;
        public string VergiNo { get; set; } = string.Empty;
        public DateTime KayitTarihi { get; set; }

        // FK
        public int EkleyenKullaniciId { get; set; }
        public User EkleyenKullanici { get; set; } = null!;

        // Navigation properties
        public ICollection<Invoice> AliciOlduguFaturalar { get; set; } = new List<Invoice>();
        public ICollection<Invoice> GondericiOlduguFaturalar { get; set; } = new List<Invoice>();
    }
}

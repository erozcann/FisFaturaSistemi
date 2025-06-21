using System.Collections.Generic;
using FisFaturaAPI.Models;

namespace FisFaturaAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string TcKimlikNo { get; set; } = string.Empty;
        public string Isim { get; set; } = string.Empty;
        public string Soyisim { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;
        public string SifreHash { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public DateTime KayitTarihi { get; set; }
        
        // Şifre sıfırlama alanları
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }

        // Navigation properties
        public ICollection<Firm> EkledigiFirmalar { get; set; } = new List<Firm>();
        public ICollection<Invoice> KaydettigiFaturalar { get; set; } = new List<Invoice>();
    }
}

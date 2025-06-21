namespace FisFaturaUI.Models
{
    public class FirmViewModel
    {
        public int Id { get; set; }
        public string FirmaAdi { get; set; } = string.Empty;
        public string VergiNo { get; set; } = string.Empty;
        public DateTime KayitTarihi { get; set; }
        public string EkleyenKullaniciAdSoyad { get; set; } = string.Empty;
    }
} 
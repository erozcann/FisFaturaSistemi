namespace FisFaturaUI.Models
{
    public class InvoiceListViewModel
    {
        public int Id { get; set; }
        public string? FirmaGonderenAdi { get; set; }
        public string? FirmaAliciAdi { get; set; }
        public string? FaturaNo { get; set; }
        public DateTime? FaturaTarihi { get; set; }
        public decimal? ToplamTutar { get; set; }
        public string? OdemeTuru { get; set; }
        public string? IcerikTuru { get; set; }
        public string? FaturaTuru { get; set; }
        public string? GelirGider { get; set; }
        public DateTime? KayitTarihi { get; set; }
        public string? FaturaResimYolu { get; set; }
    }
}

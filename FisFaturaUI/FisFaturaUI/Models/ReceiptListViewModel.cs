using System;

namespace FisFaturaUI.Models
{
    public class ReceiptListViewModel
    {
        public int Id { get; set; }
        public string? FirmaAdi { get; set; }
        public string? VergiNo { get; set; }
        public string? FisNo { get; set; }
        public DateTime? Tarih { get; set; }
        public decimal? ToplamTutar { get; set; }
        public string? IcerikTuru { get; set; }
        public string? OdemeSekli { get; set; }
        public string? GelirGider { get; set; }
        public string? FisResimYolu { get; set; }
        public DateTime KayitTarihi { get; set; }
    }
} 
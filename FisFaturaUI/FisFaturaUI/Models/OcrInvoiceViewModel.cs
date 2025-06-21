namespace FisFaturaUI.Models
{
    public class OcrInvoiceViewModel
    {
        public int Id { get; set; }
        public string FirmaAdi { get; set; } = "";
        public string VergiNo { get; set; } = "";
        public string FaturaNo { get; set; } = "";
        public string FaturaTarihi { get; set; } = "";
        public string ToplamTutar { get; set; } = "";

        public string OdemeTuru { get; set; } = "";
        public string IcerikTuru { get; set; } = "";
        public string FaturaTuru { get; set; } = "";
        public string OdemeYontemi { get; set; } = "";

        public string FirmaAliciAdi { get; set; } = "";
        public string FirmaAliciVergiNo { get; set; } = "";

        public string FirmaGonderenAdi { get; set; } = "";
        public string FirmaGonderenVergiNo { get; set; } = "";

        // Additional fields from OCR output / manual input
        public string KdvToplamTutar { get; set; } = "";
        public string Senaryo { get; set; } = "";
        public string GelirGider { get; set; } = "";
        public string FaturaTipi { get; set; } = "";
        public string Kdv0 { get; set; } = "";
        public string Matrah0 { get; set; } = "";
        public string MatrahToplamTutar { get; set; } = "";
        public string Kdv1 { get; set; } = "";
        public string Kdv8 { get; set; } = "";
        public string Kdv10 { get; set; } = "";
        public string Kdv18 { get; set; } = "";
        public string Kdv20 { get; set; } = "";
        public string Matrah1 { get; set; } = "";
        public string Matrah8 { get; set; } = "";
        public string Matrah10 { get; set; } = "";
        public string Matrah18 { get; set; } = "";
        public string Matrah20 { get; set; } = "";
        public string FaturaResimYolu { get; set; } = "";
        public IFormFile UploadedFile { get; set; } = null!;

        public int? FirmaAliciId { get; set; }
        public int? FirmaGonderenId { get; set; }
        public int? KaydedenKullaniciId { get; set; }
    }
} 
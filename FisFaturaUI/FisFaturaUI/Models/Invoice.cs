using System.ComponentModel.DataAnnotations;

namespace FisFaturaUI.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Display(Name = "Fatura No")]
        public string? FaturaNo { get; set; }

        [Display(Name = "Fatura Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? FaturaTarihi { get; set; }

        [Display(Name = "Toplam Tutar")]
        [DataType(DataType.Currency)]
        public decimal? ToplamTutar { get; set; }

        [Display(Name = "Ödeme Türü")]
        public string? OdemeTuru { get; set; }

        [Display(Name = "İçerik Türü")]
        public string? IcerikTuru { get; set; }

        public string? FaturaTuru { get; set; }
        public string? Senaryo { get; set; }
        public string? GelirGider { get; set; }
        
        public decimal? KdvToplam { get; set; }
        public decimal? MatrahToplam { get; set; }

        public decimal? Kdv_0 { get; set; }
        public decimal? Matrah_0 { get; set; }
        public decimal? Kdv_1 { get; set; }
        public decimal? Kdv_8 { get; set; }
        public decimal? Kdv_10 { get; set; }
        public decimal? Kdv_18 { get; set; }
        public decimal? Kdv_20 { get; set; }

        public decimal? Matrah_1 { get; set; }
        public decimal? Matrah_8 { get; set; }
        public decimal? Matrah_10 { get; set; }
        public decimal? Matrah_18 { get; set; }
        public decimal? Matrah_20 { get; set; }

        public DateTime? KayitTarihi { get; set; }

        public int? FirmaGonderenId { get; set; }
        public int? FirmaAliciId { get; set; }
        public int? KaydedenKullaniciId { get; set; }
        public string? FaturaResimYolu { get; set; }

        public int? FirmaID { get; set; }
        public string? OdemeYontemi { get; set; }
        public string? Aciklama { get; set; }
        public string? FaturaTipi { get; set; }
        public int UserId { get; set; }

        public string? Tip { get; set; } // 'Fatura' veya 'Fiş'
    }
} 
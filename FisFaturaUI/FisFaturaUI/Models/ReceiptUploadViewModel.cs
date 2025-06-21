using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FisFaturaUI.Models
{
    public class ReceiptUploadViewModel
    {
        public int? FirmaId { get; set; }

        [Display(Name = "Firma Adı")]
        public string? FirmaAdi { get; set; }

        [Display(Name = "Vergi Numarası")]
        public string? VergiNo { get; set; }

        [Display(Name = "Fiş Numarası")]
        public string? FisNo { get; set; }

        [Display(Name = "Tarih")]
        [DataType(DataType.Date)]
        public string? Tarih { get; set; }
        
        [Display(Name = "Toplam Tutar")]
        public string? ToplamTutar { get; set; }

        [Display(Name = "İçerik Türü")]
        public string? IcerikTuru { get; set; }

        [Display(Name = "Ödeme Şekli")]
        public string? OdemeSekli { get; set; }

        [Display(Name = "Gelir/Gider")]
        public string? GelirGider { get; set; }

        public IFormFile? Dosya { get; set; }
        
        // For dropdowns
        public List<string> IcerikTurleri { get; set; } = new List<string>();
        public List<string> OdemeSekilleri { get; set; } = new List<string>();
        public List<string> GelirGiderSecenekleri { get; set; } = new List<string>();
        
        // For KDV details
        public Dictionary<string, string> KdvOranlari { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> MatrahOranlari { get; set; } = new Dictionary<string, string>();

        public string? FaturaResimYolu { get; set; }
    }
} 
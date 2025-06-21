using System.ComponentModel.DataAnnotations;

namespace FisFaturaUI.Models
{
    public class CreateFirmViewModel
    {
        [Required(ErrorMessage = "Firma Adı alanı zorunludur.")]
        public string? FirmaAdi { get; set; }

        [Required(ErrorMessage = "Vergi/TC Kimlik Numarası alanı zorunludur.")]
        public string? VergiNo { get; set; }
    }
} 
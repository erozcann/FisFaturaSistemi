using System.ComponentModel.DataAnnotations;

namespace FisFaturaUI.Models
{
    public class EditFirmViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Firma adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Firma adı en fazla 100 karakter olabilir.")]
        [Display(Name = "Firma Adı")]
        public string FirmaAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vergi numarası zorunludur.")]
        [StringLength(11, MinimumLength = 10, ErrorMessage = "Vergi numarası 10-11 karakter olmalıdır.")]
        [Display(Name = "Vergi Numarası")]
        public string VergiNo { get; set; } = string.Empty;
    }
} 
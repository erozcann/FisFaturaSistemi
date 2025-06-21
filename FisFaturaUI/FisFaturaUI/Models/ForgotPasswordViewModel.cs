using System.ComponentModel.DataAnnotations;

namespace FisFaturaUI.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email alanı zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
        public string Email { get; set; } = string.Empty;
    }
} 
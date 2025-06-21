using System.ComponentModel.DataAnnotations;

namespace FisFaturaUI.Models
{
    public class UserLoginViewModel
    {
        [Required(ErrorMessage = "Email alanı zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        [DataType(DataType.Password)]
        public string Sifre { get; set; } = string.Empty;
    }
}

using System.ComponentModel.DataAnnotations;

namespace Jointly.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-posta gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace Jointly.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ad gereklidir")]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad gereklidir")]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir")]
        [MinLength(8, ErrorMessage = "Şifre en az 8 karakter olmalıdır")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", 
            ErrorMessage = "Şifre en az bir büyük harf, bir küçük harf ve bir rakam içermelidir")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre tekrarı gereklidir")]
        [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kullanım koşullarını kabul etmelisiniz")]
        public bool AcceptTerms { get; set; }
    }
}

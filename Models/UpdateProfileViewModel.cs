using System.ComponentModel.DataAnnotations;

namespace Jointly.Models
{
    public class UpdateProfileViewModel
    {
        [Required(ErrorMessage = "Ad zorunludur")]
        [MaxLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir")]
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad zorunludur")]
        [MaxLength(100, ErrorMessage = "Soyad en fazla 100 karakter olabilir")]
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [MaxLength(255, ErrorMessage = "E-posta en fazla 255 karakter olabilir")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;
    }
}

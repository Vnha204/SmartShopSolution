using System.ComponentModel.DataAnnotations;

namespace SmartShop.Models.ViewModels
{
    public class VerifyOtpViewModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mã OTP")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải gồm 6 chữ số")]
        [Display(Name = "Mã OTP")]
        public string OtpCode { get; set; } = string.Empty;
    }
}

using System.ComponentModel.DataAnnotations;

namespace SmartShop.Models.ViewModels
{
    public class AddAddressViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tỉnh/thành phố")]
        [MaxLength(100)]
        public string Province { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập quận/huyện")]
        [MaxLength(100)]
        public string District { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập phường/xã")]
        [MaxLength(100)]
        public string Ward { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ chi tiết")]
        [MaxLength(300)]
        public string StreetLine { get; set; } = string.Empty;

        public bool IsDefault { get; set; } = false;
    }
}

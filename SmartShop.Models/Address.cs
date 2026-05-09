using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartShop.Models
{
    public class Address
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public AppUser? User { get; set; }

        [Required, MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Province { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string District { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Ward { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string StreetLine { get; set; } = string.Empty;

        public bool IsDefault { get; set; } = false;

        public bool IsDeleted { get; set; } = false;
    }
}

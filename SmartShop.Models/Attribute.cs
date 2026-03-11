using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartShop.Models
{
    public class Attribute
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;        // 'material'

        [Required, MaxLength(150)]
        public string DisplayName { get; set; } = string.Empty; // 'Chất liệu'

        public int SortOrder { get; set; } = 0;

        // Navigation
        public ICollection<AttributeValue> Values { get; set; } = new List<AttributeValue>();
        public ICollection<CategoryAttribute> CategoryAttributes { get; set; } = new List<CategoryAttribute>();
    }
}

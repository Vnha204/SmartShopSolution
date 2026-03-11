using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartShop.Models
{
    public class AttributeValue
    {
        public int Id { get; set; }
        public int AttributeId { get; set; }
        public Attribute Attribute { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Value { get; set; } = string.Empty;       // 'titanium'

        [Required, MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty; // 'Titanium'

        public int SortOrder { get; set; } = 0;

        // Navigation
        public ICollection<ProductAttributeValue> ProductAttributeValues { get; set; } = new List<ProductAttributeValue>();
    }
}

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
        public string Value { get; set; } = string.Empty;       

        [Required, MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty; 

        public int SortOrder { get; set; } = 0;

        public ICollection<ProductAttributeValue> ProductAttributeValues { get; set; } = new List<ProductAttributeValue>();
    }
}

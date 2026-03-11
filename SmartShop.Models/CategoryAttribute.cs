using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartShop.Models
{
    public class CategoryAttribute
    {
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public int AttributeId { get; set; }
        public Attribute Attribute { get; set; } = null!;
        public int SortOrder { get; set; } = 0;
    }
}

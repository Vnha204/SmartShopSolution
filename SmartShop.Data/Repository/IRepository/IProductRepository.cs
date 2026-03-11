using SmartShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartShop.Data.Repository.IRepository
{
    public interface IProductRepository : IRepository<Product>
    {
        /// <summary>Lấy sản phẩm theo slug.</summary>
        Task<Product?> GetBySlugAsync(string slug);

        /// <summary>
        /// Dynamic filter: lọc theo category + attributes đã chọn + khoảng giá.
        /// NULL-safe — sản phẩm thiếu attribute không gây lỗi.
        /// </summary>
        Task<IEnumerable<Product>> FilterAsync(
            int categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            List<int>? attributeValueIds);

        /// <summary>Lấy sản phẩm nổi bật.</summary>
        Task<IEnumerable<Product>> GetFeaturedAsync(int take = 8);
    }
}

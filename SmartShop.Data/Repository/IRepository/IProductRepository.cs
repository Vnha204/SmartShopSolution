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
        Task<Product?> GetBySlugAsync(string slug);

        //lọc theo category,attributes đã chọn,khoảng giá.
        Task<IEnumerable<Product>> FilterAsync(
            int categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            List<int>? attributeValueIds);

        Task<IEnumerable<Product>> GetFeaturedAsync(int take = 8);

        Task<Product?> GetWithFullDetailsAsync(int id);

        Task<Product?> GetWithCategoriesAndAttributesAsync(int id);
    }
}

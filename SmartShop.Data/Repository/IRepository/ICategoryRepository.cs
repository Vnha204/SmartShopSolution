using SmartShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartShop.Data.Repository.IRepository
{
    public interface ICategoryRepository : IRepository<Category>
    {
        /// <summary>Lấy tất cả danh mục gốc (không có cha).</summary>
        Task<IEnumerable<Category>> GetRootCategoriesAsync();

        /// <summary>Lấy danh mục con theo parentId.</summary>
        Task<IEnumerable<Category>> GetChildrenAsync(int parentId);

        /// <summary>Lấy category kèm attributes để render filter sidebar.</summary>
        Task<Category?> GetWithAttributesAsync(int categoryId);

        /// <summary>Lấy category theo slug.</summary>
        Task<Category?> GetBySlugAsync(string slug);
    }
}

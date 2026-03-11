using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Net;
using SmartShop.Models;
using CategoryAttribute = SmartShop.Models.CategoryAttribute;

namespace SmartShop.Data
{
    public class SmartShopDbContext : DbContext
    {
        public SmartShopDbContext(DbContextOptions<SmartShopDbContext> options)
            : base(options) { }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Models.Attribute> Attributes => Set<Models.Attribute>();
        public DbSet<AttributeValue> AttributeValues => Set<AttributeValue>();
        public DbSet<CategoryAttribute> CategoryAttributes => Set<CategoryAttribute>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
        public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
        //public DbSet<User> Users => Set<User>();
        //public DbSet<Address> Addresses => Set<Address>();
        //public DbSet<Order> Orders => Set<Order>();
        //public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Composite keys
            modelBuilder.Entity<ProductCategory>()
                .HasKey(pc => new { pc.ProductId, pc.CategoryId });

            modelBuilder.Entity<CategoryAttribute>()
                .HasKey(ca => new { ca.CategoryId, ca.AttributeId });

            // ProductAttributeValue: unique (ProductId, AttributeValueId)
            modelBuilder.Entity<ProductAttributeValue>()
                .HasIndex(p => new { p.ProductId, p.AttributeValueId })
                .IsUnique();

            // Category self-reference: xóa cha không cascade xuống con
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
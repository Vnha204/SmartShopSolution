using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartShop.Models;
using System.ComponentModel;
using System.Net;
using CategoryAttribute = SmartShop.Models.CategoryAttribute;

namespace SmartShop.Data.Data
{
    // ⚡ Đổi từ DbContext → IdentityDbContext<AppUser>
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ===== Product System =====
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<SmartShop.Models.Attribute> Attributes => Set<SmartShop.Models.Attribute>();
        public DbSet<AttributeValue> AttributeValues => Set<AttributeValue>();
        public DbSet<CategoryAttribute> CategoryAttributes => Set<CategoryAttribute>();
        public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
        public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

        // ===== User / Order System =====
        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        // ===== Cart System =====
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();

        // ===== Contact =====
        public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

        // ===== Password Reset =====
        public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // BẮT BUỘC — để Identity tạo bảng của nó

            // Composite keys
            modelBuilder.Entity<ProductCategory>()
                .HasKey(pc => new { pc.ProductId, pc.CategoryId });

            modelBuilder.Entity<CategoryAttribute>()
                .HasKey(ca => new { ca.CategoryId, ca.AttributeId });

            modelBuilder.Entity<ProductAttributeValue>()
                .HasIndex(p => new { p.ProductId, p.AttributeValueId })
                .IsUnique();

            // Category self-reference
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order unique code
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderCode)
                .IsUnique();

            // Address soft delete filter
            modelBuilder.Entity<Address>()
                .HasQueryFilter(a => !a.IsDeleted);

            // Order → Address: no cascade (SQL Server rejects multiple cascade paths)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Address)
                .WithMany()
                .HasForeignKey(o => o.AddressId)
                .OnDelete(DeleteBehavior.NoAction);

            // One cart per user
            modelBuilder.Entity<Cart>()
                .HasIndex(c => c.UserId)
                .IsUnique();
        }
    }
}
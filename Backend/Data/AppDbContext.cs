using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }
        public DbSet<ProductSpec> ProductSpecs { get; set; }

        public DbSet<ContactSubmission> ContactSubmissions { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Product -> Specs relationship
            modelBuilder.Entity<Product>()
                .HasMany(p => p.Specs)
                .WithOne(s => s.Product)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for fast spec lookups
            modelBuilder.Entity<ProductSpec>()
                .HasIndex(s => new { s.ProductId, s.DisplayOrder });
        }
    }
}
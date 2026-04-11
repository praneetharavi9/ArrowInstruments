using Backend.Data;
using Backend.Models;
using Backend.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetAllProducts()
        {
            return await _context.Products
                                 .Where(p => p.IsActive)
                                 .Include(p => p.Specs.OrderBy(s => s.DisplayOrder))
                                 .ToListAsync();
        }

        public async Task<List<Product>> GetProductsByProductId(int productTypeId)
        {
            return await _context.Products
                                 .Where(p => p.ProductTypeId == productTypeId && p.IsActive)
                                 .Include(p => p.Specs.OrderBy(s => s.DisplayOrder))
                                 .ToListAsync();
        }

        public async Task<Product?> GetProductById(int productId)
        {
            return await _context.Products
                                 .Where(p => p.ProductId == productId && p.IsActive)
                                 .Include(p => p.Specs.OrderBy(s => s.DisplayOrder))
                                 .FirstOrDefaultAsync();
        }
    }
}
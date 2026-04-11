using Backend.Data;
using Backend.Models;
using Backend.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository
{
    public class ProductTypeRepository : IProductTypeRepository
    {
        private readonly AppDbContext _context;
        public ProductTypeRepository(AppDbContext context) {
            _context = context;
        }

        public async Task<List<ProductType>> GetAllProductTypes()
        {
            return await _context.ProductTypes.Where(pt => pt.IsActive).ToListAsync();
        }
    }
}

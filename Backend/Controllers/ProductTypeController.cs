using Backend.Data;
using Backend.Models;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductTypeController : ControllerBase
    {
        private readonly IProductTypeRepository _productTypeRepository;
        private readonly AppDbContext _context;

        public ProductTypeController(IProductTypeRepository productTypeRepository, AppDbContext context)
        {
            _productTypeRepository = productTypeRepository;
            _context = context;
        }

        // GET: api/producttype  (public — used by frontend mega-menu and products page)
        [HttpGet]
        public async Task<IActionResult> GetAllProductTypes()
        {
            var productTypes = await _productTypeRepository.GetAllProductTypes();
            return Ok(productTypes);
        }

        // POST: api/producttype  (admin only)
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] ProductTypeUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ProductTypeName))
                return BadRequest(new { success = false, message = "Category name is required." });

            var productType = new ProductType
            {
                ProductTypeName = request.ProductTypeName.Trim(),
                ProductTypeDescription = request.ProductTypeDescription?.Trim(),
                IsActive = true,
                DateCreated = DateTime.UtcNow
            };

            _context.ProductTypes.Add(productType);
            await _context.SaveChangesAsync();

            return Ok(productType);
        }

        // PUT: api/producttype/5  (admin only)
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductTypeUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ProductTypeName))
                return BadRequest(new { success = false, message = "Category name is required." });

            var productType = await _context.ProductTypes.FindAsync(id);
            if (productType == null)
                return NotFound(new { success = false, message = $"Product type {id} not found." });

            productType.ProductTypeName = request.ProductTypeName.Trim();
            productType.ProductTypeDescription = request.ProductTypeDescription?.Trim();
            productType.DateUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(productType);
        }

        // DELETE: api/producttype/5  (admin only)
        // Cascades: specs → products → product type, all inside one transaction.
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var productType = await _context.ProductTypes.FindAsync(id);
            if (productType == null)
                return NotFound(new { success = false, message = $"Product type {id} not found." });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Find all products in this category
                var productIds = await _context.Products
                    .Where(p => p.ProductTypeId == id)
                    .Select(p => p.ProductId)
                    .ToListAsync();

                // 2. Delete all specs for those products
                if (productIds.Count > 0)
                {
                    var specs = await _context.ProductSpecs
                        .Where(s => productIds.Contains(s.ProductId))
                        .ToListAsync();
                    _context.ProductSpecs.RemoveRange(specs);

                    // 3. Delete all products in this category
                    var products = await _context.Products
                        .Where(p => p.ProductTypeId == id)
                        .ToListAsync();
                    _context.Products.RemoveRange(products);
                }

                // 4. Delete the product type itself
                _context.ProductTypes.Remove(productType);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Product type and all its products deleted successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = $"Delete failed: {ex.Message}" });
            }
        }
    }

    public class ProductTypeUpsertRequest
    {
        public string ProductTypeName { get; set; } = string.Empty;
        public string? ProductTypeDescription { get; set; }
    }
}

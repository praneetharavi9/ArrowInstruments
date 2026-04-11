using Backend.Data;
using Backend.Models;
using Backend.Repository.Interfaces;
using FluentFTP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly AppDbContext _context;
        private readonly FtpSettings _ftpSettings;

        public ProductsController(
            IProductRepository productRepository,
            AppDbContext context,
            IOptions<FtpSettings> ftpSettings)
        {
            _productRepository = productRepository;
            _context = context;
            _ftpSettings = ftpSettings.Value;
        }

        // GET: api/products  (public)
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _productRepository.GetAllProducts();
            return Ok(products);
        }

        // GET: api/products/5  (public)
        [HttpGet("{productId}")]
        public async Task<IActionResult> GetProductById(int productId)
        {
            var product = await _productRepository.GetProductById(productId);
            if (product == null)
                return NotFound(new { message = $"Product {productId} not found." });

            return Ok(product);
        }

        // GET: api/products/GetProductsByProductId/2  (public)
        [HttpGet("GetProductsByProductId/{productTypeId}")]
        public async Task<IActionResult> GetProductsByProductId(int productTypeId)
        {
            var products = await _productRepository.GetProductsByProductId(productTypeId);
            return Ok(products);
        }

        // POST: api/products  (admin only)
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] ProductUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ProductName))
                return BadRequest(new { success = false, message = "Product name is required." });

            var product = new Product
            {
                ProductName = request.ProductName.Trim(),
                ProductDescription = request.ProductDescription?.Trim(),
                ProductTypeId = request.ProductTypeId,
                IsActive = request.IsActive,
                DateCreated = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Save specs if provided
            if (request.Specs != null && request.Specs.Count > 0)
            {
                var specs = request.Specs
                    .Where(s => !string.IsNullOrWhiteSpace(s.SpecName))
                    .Select((s, i) => new ProductSpec
                    {
                        ProductId = product.ProductId,
                        SpecName = s.SpecName.Trim(),
                        SpecValue = s.SpecValue?.Trim() ?? string.Empty,
                        DisplayOrder = s.DisplayOrder != 0 ? s.DisplayOrder : i
                    }).ToList();

                _context.ProductSpecs.AddRange(specs);
                await _context.SaveChangesAsync();

                product.Specs = specs;
            }

            return Ok(product);
        }

        // PUT: api/products/5  (admin only)
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ProductName))
                return BadRequest(new { success = false, message = "Product name is required." });

            var product = await _context.Products
                .Include(p => p.Specs)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound(new { success = false, message = $"Product {id} not found." });

            product.ProductName = request.ProductName.Trim();
            product.ProductDescription = request.ProductDescription?.Trim();
            product.ProductTypeId = request.ProductTypeId;
            product.IsActive = request.IsActive;
            product.DateUpdated = DateTime.UtcNow;

            // Replace specs: remove old, add new
            if (request.Specs != null)
            {
                _context.ProductSpecs.RemoveRange(product.Specs);

                var newSpecs = request.Specs
                    .Where(s => !string.IsNullOrWhiteSpace(s.SpecName))
                    .Select((s, i) => new ProductSpec
                    {
                        ProductId = id,
                        SpecName = s.SpecName.Trim(),
                        SpecValue = s.SpecValue?.Trim() ?? string.Empty,
                        DisplayOrder = s.DisplayOrder != 0 ? s.DisplayOrder : i
                    }).ToList();

                _context.ProductSpecs.AddRange(newSpecs);
                product.Specs = newSpecs;
            }

            await _context.SaveChangesAsync();

            return Ok(product);
        }

        // DELETE: api/products/5  (admin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { success = false, message = $"Product {id} not found." });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Delete specs first to satisfy the FK constraint on product_specs.product_id
                var specs = await _context.ProductSpecs
                    .Where(s => s.ProductId == id)
                    .ToListAsync();
                _context.ProductSpecs.RemoveRange(specs);

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Product deleted successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = $"Delete failed: {ex.Message}" });
            }
        }

        // POST: api/products/5/image  (admin only)
        [HttpPost("{id}/image")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UploadImage(int id, IFormFile image)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { success = false, message = $"Product {id} not found." });

            if (image == null || image.Length == 0)
                return BadRequest(new { success = false, message = "No image file provided." });

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowedTypes.Contains(image.ContentType.ToLower()))
                return BadRequest(new { success = false, message = "Only JPEG, PNG, WebP, or GIF images are allowed." });

            var ext = Path.GetExtension(image.FileName).ToLower();
            var fileName = $"{id}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";

            // Buffer the upload into memory before opening the FTP connection
            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);
            ms.Position = 0;

            var remotePath = _ftpSettings.RemotePath.TrimEnd('/') + "/" + fileName;

            try
            {
                using var ftp = new AsyncFtpClient(
                    _ftpSettings.Host,
                    _ftpSettings.User,
                    _ftpSettings.Password,
                    _ftpSettings.Port);

                // Accept HostGator's shared-hosting SSL certificate
                ftp.Config.ValidateAnyCertificate = true;
                // Let FluentFTP auto-detect FTPS/FTP
                ftp.Config.EncryptionMode = FtpEncryptionMode.Auto;

                await ftp.Connect();
                var status = await ftp.UploadStream(
                    ms, remotePath,
                    FtpRemoteExists.Overwrite,
                    createRemoteDir: true);
                await ftp.Disconnect();

                if (status == FtpStatus.Failed)
                    return StatusCode(500, new { success = false, message = "FTP upload failed — file was not transferred." });
            }
            catch (Exception ex)
            {
                // Return the real FTP error so it is visible in the browser / Swagger
                return StatusCode(500, new
                {
                    success = false,
                    message = $"FTP error: {ex.Message}",
                    detail = ex.InnerException?.Message
                });
            }

            product.ImagePath = fileName;
            product.DateUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(product);
        }
    }

    public class ProductUpsertRequest
    {
        public string ProductName { get; set; } = string.Empty;
        public string? ProductDescription { get; set; }
        public int ProductTypeId { get; set; }
        public bool IsActive { get; set; } = true;
        public List<SpecRequest>? Specs { get; set; }
    }

    public class SpecRequest
    {
        public string SpecName { get; set; } = string.Empty;
        public string? SpecValue { get; set; }
        public int DisplayOrder { get; set; }
    }
}

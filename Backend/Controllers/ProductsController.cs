using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepository;

        public ProductsController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        // GET: api/products
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _productRepository.GetAllProducts();
            return Ok(products);
        }

        [HttpGet]
        [Route("GetProductsByProductId/{productTypeId}")]
        public async Task<IActionResult> GetProductsByProductId(int productTypeId)
        {
            var products = await _productRepository.GetProductsByProductId(productTypeId);
            return Ok(products);
        }

    }
}

using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductTypeController : ControllerBase
    {
        private IProductTypeRepository _productTypeRepository;
        public ProductTypeController(IProductTypeRepository productTypeRepository)
        {
            _productTypeRepository = productTypeRepository;
        }
        // GET: api/producttype
        [HttpGet]
        public IActionResult GetAllProductTypes()
        {
            var productTypes = _productTypeRepository.GetAllProductTypes();
            return Ok(productTypes);
        }
    }
}

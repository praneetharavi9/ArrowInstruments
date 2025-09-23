using Backend.Models;

namespace Backend.Repository.Interfaces
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllProducts();
    }
}

using Backend.Models;

namespace Backend.Repository.Interfaces
{
    public interface IProductTypeRepository
    {
        Task<List<ProductType>> GetAllProductTypes();
    }
}

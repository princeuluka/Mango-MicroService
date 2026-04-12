using Mango.Services.OrderAPI.Models.Dto;

namespace Mango.Services.OrderAPI.Service.IService
{
    public interface IProductService
    {
        Task<ProductDto> GetProductById(int productId);
        Task<IEnumerable<ProductDto>> GetProducts();
    }
}

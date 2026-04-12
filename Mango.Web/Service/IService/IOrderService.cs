using Mango.Web.Models;

namespace Mango.Web.Service.IService
{
    public interface IOrderService
    {
        Task<ResponseDto> CreateOrder(CartDto cartDto);
        Task<ResponseDto> GetOrders(string userId);
        Task<ResponseDto> GetOrder(int orderId);
        Task<ResponseDto> UpdateOrderStatus(int orderId, string newStatus);
    }
}

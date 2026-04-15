using Mango.Services.EmailAPI.Models.Dto;

namespace Mango.Service.EmailAPI.Services
{
    public interface IEmailService
    {
        Task EmailCartAndLog(CartDto cartDto);
        Task EmailCheckoutAndLog(CheckoutHeaderDto checkoutHeader);
        Task EmailOrderCreatedAndLog(OrderHeaderDto orderHeader);
    }
}

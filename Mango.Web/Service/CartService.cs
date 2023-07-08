using Mango.Web.Models;
using Mango.Web.Service.IService;
using Mango.Web.Utility;

namespace Mango.Web.Service
{
    public class CartService : ICartService
    {
        private readonly IBaseService _baseService;
        public CartService(IBaseService baseService)
        {
            _baseService = baseService;
        }
        public async Task<ResponseDto> ApplyCouponAsync(CartDto cartDto)
        {
            return await _baseService.SendAsync(new RequestDto()
            {
                ApiType = Utility.SD.Apitype.POST,
                Url = SD.CartAPIBase + "/api/cart/ApplyCoupon",
                Data = cartDto

            });
        }

        public async Task<ResponseDto> GetCartByUSerIdAsync(string userId)
        {
            return await _baseService.SendAsync(new RequestDto()
            {
                ApiType = Utility.SD.Apitype.GET,
                Url = SD.CartAPIBase + $"/api/cart/GetCart/{userId}",
                });
        }

        public async Task<ResponseDto> RemoveFromCartAsync(string cartDetailsId)
        {
            return await _baseService.SendAsync(new RequestDto()
            {
                ApiType = Utility.SD.Apitype.POST,
                Url = SD.CartAPIBase + $"/api/cart/RemoveCart",
                Data = cartDetailsId
            });
        }

        public async Task<ResponseDto> UpsertCartAsync(CartDto cartDto)
        {
            return await _baseService.SendAsync(new RequestDto()
            {
                ApiType = Utility.SD.Apitype.POST,
                Url = SD.CartAPIBase + $"/api/cart/CartUpsert",
                Data = cartDto
            });
        }
    }
}

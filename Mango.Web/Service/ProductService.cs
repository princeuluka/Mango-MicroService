using Mango.Web.Models;
using Mango.Web.Service.IService;
using Mango.Web.Utility;

namespace Mango.Web.Service
{
    public class ProductService : IProductService
    {
        private readonly IBaseService _baseService;
        public ProductService(IBaseService baseService)
        {
            _baseService = baseService;
        }
        public async Task<ResponseDto> CreateProductAsync(ProductDto productDto)
        {
            return await _baseService.SendAsync(new RequestDto()
            {
                ApiType = Utility.SD.Apitype.POST,
                Url = SD.ProductAPIBase + "/api/ProductAPI",
                Data = productDto

            });
        }

        public async Task<ResponseDto> DeleteProductAsync(int id)
        {
            return await _baseService.SendAsync(new RequestDto()
            {
                ApiType = Utility.SD.Apitype.DELETE,
                Url = SD.ProductAPIBase + "/api/ProductAPI/" + id
            });
        }

        public async Task<ResponseDto> GetAllProductsAsync()
        {
            return await _baseService.SendAsync(new RequestDto()
            {
                ApiType = Utility.SD.Apitype.GET,
                Url = SD.ProductAPIBase + "/api/ProductAPI"
            });
        }

        public async Task<ResponseDto> GetProducsAsync(string categoryName)
        {
            return await _baseService.SendAsync(new RequestDto()
            {
                ApiType = Utility.SD.Apitype.GET,
                Url = SD.ProductAPIBase + "/api/ProductAPI/GetByCategoryName/" + categoryName
            });
        }

        public async Task<ResponseDto> GetProductByIdAsync(int id)
        {
            return await _baseService.SendAsync(new RequestDto()
            {
                ApiType = Utility.SD.Apitype.GET,
                Url = SD.ProductAPIBase + "/api/ProductAPI/" + id
            });
        }

        public async Task<ResponseDto> UpdateProductAsync(ProductDto productDto)
        {
            return await _baseService.SendAsync(new RequestDto()
            {
                ApiType = Utility.SD.Apitype.PUT,
                Url = SD.ProductAPIBase + "/api/ProductAPI",
                Data = productDto

            });
        }
    }
}


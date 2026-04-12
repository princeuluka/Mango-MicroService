using AutoMapper;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;

namespace Mango.Services.OrderAPI
{
    public class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            var mappingConfig = new MapperConfiguration(config =>
            {
                config.CreateMap<OrderHeaderDto, OrderHeader>().ReverseMap();
                config.CreateMap<OrderDetailsDto, OrderDetails>().ReverseMap();
                config.CreateMap<CartHeaderDto, OrderHeaderDto>()
                    .ForMember(dest => dest.OrderTotal, u => u.MapFrom(src => src.CartTotal));
                config.CreateMap<CartDetailsDto, OrderDetailsDto>();
            });
            return mappingConfig;
        }
    }
}

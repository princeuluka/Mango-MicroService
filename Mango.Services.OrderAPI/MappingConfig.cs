using AutoMapper;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;

namespace Mango.Services.OrderAPI
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            CreateMap<OrderHeader, OrderHeaderDto>().ReverseMap();
            CreateMap<OrderDetails, OrderDetailsDto>().ReverseMap();
            CreateMap<CartHeaderDto, OrderHeaderDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.CouponCode, opt => opt.MapFrom(src => src.CouponCode))
                .ForMember(dest => dest.Discount, opt => opt.MapFrom(src => src.Discount))
                .ForMember(dest => dest.OrderTotal, opt => opt.MapFrom(src => src.CartTotal))
                .ForMember(dest => dest.PickupEmail, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.PickupName, opt => opt.MapFrom(src => src.FirstName + " " + src.LastName))
                .ForMember(dest => dest.PickupPhone, opt => opt.MapFrom(src => src.Phone));
        }

        public static MapperConfiguration RegisterMaps()
        {
            return new MapperConfiguration(config =>
            {
                config.AddProfile(new MappingConfig());
            });
        }
    }
}

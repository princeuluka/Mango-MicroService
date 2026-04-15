using AutoMapper;
using Mango.Services.RewardAPI.Models;
using Mango.Services.RewardAPI.Models.Dto;

namespace Mango.Services.RewardAPI
{
    public class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            var mappingConfig = new MapperConfiguration(config =>
            {
                config.CreateMap<RewardActivity, RewardActivityDto>();
                config.CreateMap<RewardActivityDto, RewardActivity>();
            });
            return mappingConfig;
        }
    }
}

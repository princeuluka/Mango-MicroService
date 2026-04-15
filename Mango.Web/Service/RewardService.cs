using Mango.Web.Models;
using Mango.Web.Service.IService;
using Mango.Web.Utility;

namespace Mango.Web.Service
{
    public class RewardService : IRewardService
    {
        private readonly IBaseService _baseService;

        public RewardService(IBaseService baseService)
        {
            _baseService = baseService;
        }

        public async Task<ResponseDto> GetRewardPointsAsync(string userId)
        {
            return await _baseService.SendAsync(new RequestDto()
            {
                ApiType = SD.Apitype.GET,
                Url = SD.RewardAPIBase + "/api/RewardAPI/GetPoints/" + userId
            });
        }

        public async Task<ResponseDto> GetRewardHistoryAsync(string userId)
        {
            return await _baseService.SendAsync(new RequestDto()
            {
                ApiType = SD.Apitype.GET,
                Url = SD.RewardAPIBase + "/api/RewardAPI/GetHistory/" + userId
            });
        }
    }
}

using Mango.Web.Models;

namespace Mango.Web.Service.IService
{
    public interface IRewardService
    {
        Task<ResponseDto> GetRewardPointsAsync(string userId);
        Task<ResponseDto> GetRewardHistoryAsync(string userId);
    }
}

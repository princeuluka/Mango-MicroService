using Mango.Web.Models;

namespace Mango.Web.Service.IService
{
    public interface IRewardService
    {
        Task<ResponseDto> GetRewards(string? userId);
    }
}

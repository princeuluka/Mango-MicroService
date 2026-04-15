using AutoMapper;
using Mango.Services.RewardAPI.Data;
using Mango.Services.RewardAPI.Models;
using Mango.Services.RewardAPI.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mango.Services.RewardAPI.Controllers
{
    [Route("api/reward")]
    [ApiController]
    public class RewardAPIController : ControllerBase
    {
        private readonly AppDbContext _db;
        protected ResponseDto _response;
        private IMapper _mapper;

        public RewardAPIController(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _response = new ResponseDto();
            _mapper = mapper;
        }

        [HttpGet("GetRewards")]
        [Authorize]
        public ResponseDto Get(string? userId = "")
        {
            try
            {
                IEnumerable<Rewards> objList;
                if (string.IsNullOrEmpty(userId))
                {
                    objList = _db.Rewards.OrderByDescending(u => u.RewardsDate).ToList();
                }
                else
                {
                    objList = _db.Rewards.Where(u => u.UserId == userId)
                                .OrderByDescending(u => u.RewardsDate).ToList();
                }
                _response.Result = _mapper.Map<IEnumerable<RewardDto>>(objList);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }
    }
}

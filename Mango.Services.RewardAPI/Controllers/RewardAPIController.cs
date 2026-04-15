using AutoMapper;
using Mango.Services.RewardAPI.Data;
using Mango.Services.RewardAPI.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.RewardAPI.Controllers
{
    [ApiController]
    [Route("api/RewardAPI")]
    [Authorize]
    public class RewardAPIController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        private ResponseDto _response;

        public RewardAPIController(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
            _response = new ResponseDto();
        }

        [HttpGet]
        [Route("GetPoints/{userId}")]
        public async Task<ResponseDto> GetPoints(string userId)
        {
            try
            {
                var totalPoints = await _db.RewardActivities
                    .Where(r => r.UserId == userId)
                    .SumAsync(r => r.Points);

                _response.Result = new RewardPointsDto
                {
                    UserId = userId,
                    TotalPoints = totalPoints
                };
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpGet]
        [Route("GetHistory/{userId}")]
        public async Task<ResponseDto> GetHistory(string userId)
        {
            try
            {
                var activities = await _db.RewardActivities
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.ActivityDate)
                    .ToListAsync();

                _response.Result = _mapper.Map<IEnumerable<RewardActivityDto>>(activities);
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

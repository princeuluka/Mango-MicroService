using Mango.Web.Models;
using Mango.Web.Service.IService;
using Mango.Web.Utility;

namespace Mango.Web.Service
{
    public class AuthService : IAuthService
    {
        private readonly IBaseService _baseService;
        public AuthService(IBaseService baseService)
        {
            _baseService = baseService;
        }
        public async Task<ResponseDto> AssignRoleAsync(RegistrationRequestDTO registrationRequestDTO)
        {
            return await _baseService.SendAsync(new RequestDto
            {
                ApiType = Utility.SD.Apitype.POST,
                Url = SD.AuthAPIBase + "/api/auth/assignrole",
                Data = registrationRequestDTO
            });
        }

        public async Task<ResponseDto> LoginAsync(LoginRequestDTO loginRequestDTO)
        {
            return await _baseService.SendAsync(new RequestDto
            {
                ApiType = Utility.SD.Apitype.POST,
                Url = SD.AuthAPIBase + "/api/auth/login",
                Data = loginRequestDTO
            }, withBearer: false);
        }

        public async Task<ResponseDto> RegisterAsync(RegistrationRequestDTO registrationRequestDTO)
        {
            return await _baseService.SendAsync(new RequestDto
            {
                ApiType = Utility.SD.Apitype.POST,
                Url = SD.AuthAPIBase + "/api/auth/register",
                Data = registrationRequestDTO
            }, withBearer: false);
        }
    }
}

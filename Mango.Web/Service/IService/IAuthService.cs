using Mango.Web.Models;

namespace Mango.Web.Service.IService
{
    public interface IAuthService
    {
        Task<ResponseDto> LoginAsync(LoginRequestDTO loginRequestDTO);
        Task<ResponseDto> RegisterAsync(RegistrationRequestDTO registrationRequestDTO);
        Task<ResponseDto> AssignRoleAsync(RegistrationRequestDTO registrationRequestDTO);
    }
}

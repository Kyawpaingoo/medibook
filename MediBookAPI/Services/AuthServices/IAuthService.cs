using Data.Dtos;

namespace MediBookAPI.Services.AuthServices;

public interface IAuthService
{
    Task<(string Status, AuthResponseDto? Result)> RegisterAsync(RegisterRequestDtos dto);
    Task<(string Status, AuthResponseDto? Result)> LoginAsync(LoginRequestDtos dto);
    Task<(string Status, AuthResponseDto? Result)> RefreshTokenAsync(RefreshTokenRequestDtos dto);
    Task<string> RevokeRefreshTokenAsync(Guid userId);
}
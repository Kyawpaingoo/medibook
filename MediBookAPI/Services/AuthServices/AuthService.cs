using System.IdentityModel.Tokens.Jwt;
using Data.Dtos;
using Data.Enums;
using Data.Models;
using Infra.Services.JwtTokens;
using Infra.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace MediBookAPI.Services.AuthServices;

public class AuthService: IAuthService
{
    private readonly IBookingUnitOfWork _uow;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IBookingUnitOfWork uow, ITokenService tokenService, ILogger<AuthService> logger)
    {
        _uow = uow;
        _tokenService = tokenService;
        _logger = logger;
    }
    
    public async Task<(string Status, AuthResponseDto? Result)> RegisterAsync(RegisterRequestDtos dto)
    {
        var emailTaken = await _uow.Users.AnyAsync(u => u.Email == dto.Email);

        if (emailTaken) return (ResponseStatus.Fail, null);

        var newUser = new tbUsers
        {
            Email = dto.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            Doctor_Id = dto.Doctor_Id,
            Created_At = DateTimeOffset.Now
        };

        try
        {
            var token = IssueToken(newUser);
            
            await _uow.Users.AddAsync(newUser);
            await _uow.SaveChangeAsync();
            return (ResponseStatus.Success, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register user with email {Email}", dto.Email);
            return (ResponseStatus.Fail, null);
        }
    }

    public async Task<(string Status, AuthResponseDto? Result)> LoginAsync(LoginRequestDtos dto)
    {
        var user = await _uow.Users.GetAll().Where(u => u.Email == dto.Email).FirstOrDefaultAsync();
        
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
            return (ResponseStatus.Fail, null);

        try
        {
            var token = IssueToken(user);
            await _uow.Users.UpdateAsync(user);
            await _uow.SaveChangeAsync();
            return (ResponseStatus.Success, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log in user with email {Email}", dto.Email);
            return (ResponseStatus.Fail, null);
        }
    }

    public async Task<(string Status, AuthResponseDto? Result)> RefreshTokenAsync(RefreshTokenRequestDtos dto)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(dto.AccessToken);
        
        var subject = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (subject is null || !Guid.TryParse(subject, out var userId))
            return (ResponseStatus.Fail, null);

        var user = await _uow.Users.GetByIdAsync(userId);
        
        if(user is null || user.Refresh_Token != dto.RefreshToken || user.Refresh_Token_Expiry is null || user.Refresh_Token_Expiry <= DateTime.UtcNow)
            return (ResponseStatus.Fail, null);

        try
        {
            var tokens = IssueToken(user);
            await _uow.Users.UpdateAsync(user);
            await _uow.SaveChangeAsync();
            return (ResponseStatus.Success, tokens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh token for user {UserId}", userId);
            return (ResponseStatus.Fail, null);
        }
    }

    public async Task<string> RevokeRefreshTokenAsync(Guid userId)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user is null) return ResponseStatus.DoestNotExist;
        user.Refresh_Token = null;
        user.Refresh_Token_Expiry = null;
        try
        {
            await _uow.Users.UpdateAsync(user);
            await _uow.SaveChangeAsync();
            return ResponseStatus.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke refresh token for user {UserId}", userId);
            return ResponseStatus.Fail;
        }

    }

    private AuthResponseDto IssueToken(tbUsers user)
    {
        var (accessToken, accessExpiry) = _tokenService.GenerateAccessToken(user);

        var (refreshToken, refreshExpiry) = _tokenService.GenerateRefreshToken();

        user.Refresh_Token = refreshToken;
        user.Refresh_Token_Expiry = refreshExpiry;

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiry = accessExpiry
        };
    }
}
using System.Security.Claims;
using Data.Models;

namespace Infra.Services.JwtTokens;

public interface ITokenService
{
    (string Token, DateTime Expiration) GenerateAccessToken(tbUsers user);
    (string Token, DateTime Expiration) GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
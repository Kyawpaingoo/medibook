using Data.Enums;

namespace Data.Dtos;

public class RegisterRequestDtos
{
    public required string Email  { get; set; }
    public required string Password { get; set; }
    public required UserRole Role { get; set; }
    public Guid? Doctor_Id { get; set; }
}

public class LoginRequestDtos
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class RefreshTokenRequestDtos
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}

public class AuthResponseDto
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public DateTime AccessTokenExpiry { get; set; }
}
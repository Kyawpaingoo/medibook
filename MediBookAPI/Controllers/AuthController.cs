using System.Security.Claims;
using Data.Dtos;
using Data.Enums;
using MediBookAPI.Services.AuthServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediBookAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync(RegisterRequestDtos dto)
    {
        var (status, result) = await _authService.RegisterAsync(dto);
        return ToActionResult(status, result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync(LoginRequestDtos dto)
    {
        var (status, result) = await _authService.LoginAsync(dto);
        return ToActionResult(status, result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshTokenAsync(RefreshTokenRequestDtos dto)
    {
        var (status, result) = await _authService.RefreshTokenAsync(dto);
        return status == ResponseStatus.Success ? Ok(result) : Unauthorized(status);
    }

    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeRefreshTokenAsync()
    {
        var subject = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

        if (subject is null || !Guid.TryParse(subject, out var userId))
            return Unauthorized();
        
        var result = await _authService.RevokeRefreshTokenAsync(userId);

        return result switch
        {
            ResponseStatus.Success => NoContent(),
            ResponseStatus.DoestNotExist => NotFound(result),
            _ => StatusCode(StatusCodes.Status500InternalServerError, result)
        };
    }
    
    private IActionResult ToActionResult(string status, AuthResponseDto? result) => status switch
    {
        ResponseStatus.Success => Ok(result),
        ResponseStatus.Fail => BadRequest(status),
        _ => StatusCode(StatusCodes.Status500InternalServerError, status)
    };
}
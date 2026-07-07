using Data.Dtos;
using Data.Enums;
using Data.Models;
using Infra.Services.JwtTokens;
using Infra.UnitOfWork;
using MediBookAPI.Services.AuthServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace UnitTesting
{
    public class AuthServiceTests
    {
        private static (AuthService Service, BookingDBContext Context) CreateService()
        {
            var options = new DbContextOptionsBuilder<BookingDBContext>()
                            .UseInMemoryDatabase(Guid.NewGuid().ToString())
                            .Options;

            var context = new BookingDBContext(options);
            var uow = new BookingUnitOfWork(context);
            var tokenService = new TokenService(Options.Create(new JwtSettings
            {
                Key = "unit-test-signing-key-at-least-32-characters-long",
                Issuer = "MediBookAPI.Tests",
                Audience = "MediBookAPI.Tests",
                AccessTokenExpiration = 15,
                RefreshTokenExpiration = 7
            }));
            var service = new AuthService(uow, tokenService, NullLogger<AuthService>.Instance!);

            return (service, context);
        }

        private static tbUsers NewUser(
            string email = "jane.doe@medibook.test",
            string plainTextPassword = "P@ssw0rd123",
            UserRole role = UserRole.Staff,
            string? refreshToken = null,
            DateTime? refreshTokenExpiry = null
        )
        {
            return new tbUsers
            {
                Id = Guid.NewGuid(),
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(plainTextPassword),
                Role = role,
                Refresh_Token = refreshToken,
                Refresh_Token_Expiry = refreshTokenExpiry,
                Created_At = DateTimeOffset.UtcNow
            };
        }

        [Fact]
        public async Task RegisterAsync_NewEmail_CreatesHashedUserAndReturnsTokens()
        {
            var (service, context) = CreateService();
            var dto = new RegisterRequestDtos
            {
                Email = "new.user@medibook.test",
                Password = "P@ssw0rd123",
                Role = UserRole.Staff
            };

            var (status, result) = await service.RegisterAsync(dto);

            Assert.Equal(ResponseStatus.Success, status);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result!.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));

            var saved = await context.tbUsers.SingleAsync();
            Assert.Equal(dto.Email, saved.Email);
            Assert.NotEqual(dto.Password, saved.Password);
            Assert.True(BCrypt.Net.BCrypt.Verify(dto.Password, saved.Password));
            Assert.Equal(result.RefreshToken, saved.Refresh_Token);
        }

        [Fact]
        public async Task RegisterAsync_DuplicateEmail_ReturnsFail()
        {
            var (service, context) = CreateService();
            context.tbUsers.Add(NewUser(email: "taken@medibook.test"));
            await context.SaveChangesAsync();

            var (status, result) = await service.RegisterAsync(new RegisterRequestDtos
            {
                Email = "taken@medibook.test",
                Password = "AnotherP@ss1",
                Role = UserRole.Staff
            });

            Assert.Equal(ResponseStatus.Fail, status);
            Assert.Null(result);
            Assert.Equal(1, await context.tbUsers.CountAsync());
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsTokensAndPersistsRefreshToken()
        {
            var (service, context) = CreateService();
            var user = NewUser(email: "login@medibook.test", plainTextPassword: "correct-password");
            context.tbUsers.Add(user);
            await context.SaveChangesAsync();

            var (status, result) = await service.LoginAsync(new LoginRequestDtos
            {
                Email = "login@medibook.test",
                Password = "correct-password"
            });

            Assert.Equal(ResponseStatus.Success, status);
            Assert.NotNull(result);

            var updated = await context.tbUsers.SingleAsync(u => u.Id == user.Id);
            Assert.Equal(result!.RefreshToken, updated.Refresh_Token);
            Assert.NotNull(updated.Refresh_Token_Expiry);
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ReturnsFail()
        {
            var (service, context) = CreateService();
            context.tbUsers.Add(NewUser(email: "login2@medibook.test", plainTextPassword: "correct-password"));
            await context.SaveChangesAsync();

            var (status, result) = await service.LoginAsync(new LoginRequestDtos
            {
                Email = "login2@medibook.test",
                Password = "wrong-password"
            });

            Assert.Equal(ResponseStatus.Fail, status);
            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_UnknownEmail_ReturnsFail()
        {
            var (service, _) = CreateService();

            var (status, result) = await service.LoginAsync(new LoginRequestDtos
            {
                Email = "ghost@medibook.test",
                Password = "whatever"
            });

            Assert.Equal(ResponseStatus.Fail, status);
            Assert.Null(result);
        }

        [Fact]
        public async Task RefreshTokenAsync_ValidAccessAndMatchingRefreshToken_RotatesTokens()
        {
            var (service, context) = CreateService();
            var user = NewUser(email: "refresh@medibook.test", plainTextPassword: "pw");
            context.tbUsers.Add(user);
            await context.SaveChangesAsync();

            var (_, loginResult) = await service.LoginAsync(new LoginRequestDtos { Email = user.Email, Password = "pw" });

            var (status, result) = await service.RefreshTokenAsync(new RefreshTokenRequestDtos
            {
                AccessToken = loginResult!.AccessToken,
                RefreshToken = loginResult.RefreshToken
            });

            Assert.Equal(ResponseStatus.Success, status);
            Assert.NotNull(result);
            Assert.NotEqual(loginResult.RefreshToken, result!.RefreshToken);

            var updated = await context.tbUsers.SingleAsync(u => u.Id == user.Id);
            Assert.Equal(result.RefreshToken, updated.Refresh_Token);
        }

        [Fact]
        public async Task RefreshTokenAsync_MismatchedRefreshToken_ReturnsFail()
        {
            var (service, context) = CreateService();
            var user = NewUser(email: "refresh2@medibook.test", plainTextPassword: "pw");
            context.tbUsers.Add(user);
            await context.SaveChangesAsync();

            var (_, loginResult) = await service.LoginAsync(new LoginRequestDtos { Email = user.Email, Password = "pw" });

            var (status, result) = await service.RefreshTokenAsync(new RefreshTokenRequestDtos
            {
                AccessToken = loginResult!.AccessToken,
                RefreshToken = "a-completely-different-refresh-token"
            });

            Assert.Equal(ResponseStatus.Fail, status);
            Assert.Null(result);
        }

        [Fact]
        public async Task RefreshTokenAsync_ExpiredRefreshToken_ReturnsFail()
        {
            var (service, context) = CreateService();
            var user = NewUser(
                email: "refresh3@medibook.test",
                plainTextPassword: "pw",
                refreshToken: "already-expired-token",
                refreshTokenExpiry: DateTime.UtcNow.AddDays(-1));
            context.tbUsers.Add(user);
            await context.SaveChangesAsync();

            var tokenService = new TokenService(Options.Create(new JwtSettings
            {
                Key = "unit-test-signing-key-at-least-32-characters-long",
                Issuer = "MediBookAPI.Tests",
                Audience = "MediBookAPI.Tests",
                AccessTokenExpiration = 15,
                RefreshTokenExpiration = 7
            }));
            var (accessToken, _) = tokenService.GenerateAccessToken(user);

            var (status, result) = await service.RefreshTokenAsync(new RefreshTokenRequestDtos
            {
                AccessToken = accessToken,
                RefreshToken = "already-expired-token"
            });

            Assert.Equal(ResponseStatus.Fail, status);
            Assert.Null(result);
        }

        [Fact]
        public async Task RefreshTokenAsync_MalformedAccessToken_ReturnsFailInsteadOfThrowing()
        {
            var (service, _) = CreateService();

            var (status, result) = await service.RefreshTokenAsync(new RefreshTokenRequestDtos
            {
                AccessToken = "not-a-real-jwt",
                RefreshToken = "irrelevant"
            });

            Assert.Equal(ResponseStatus.Fail, status);
            Assert.Null(result);
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_ExistingUser_ClearsStoredRefreshToken()
        {
            var (service, context) = CreateService();
            var user = NewUser(
                email: "revoke@medibook.test",
                refreshToken: "existing-refresh-token",
                refreshTokenExpiry: DateTime.UtcNow.AddDays(1));
            context.tbUsers.Add(user);
            await context.SaveChangesAsync();

            var status = await service.RevokeRefreshTokenAsync(user.Id);

            Assert.Equal(ResponseStatus.Success, status);
            var updated = await context.tbUsers.SingleAsync(u => u.Id == user.Id);
            Assert.Null(updated.Refresh_Token);
            Assert.Null(updated.Refresh_Token_Expiry);
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_UnknownUser_ReturnsDoesNotExist()
        {
            var (service, _) = CreateService();

            var status = await service.RevokeRefreshTokenAsync(Guid.NewGuid());

            Assert.Equal(ResponseStatus.DoestNotExist, status);
        }
    }
}

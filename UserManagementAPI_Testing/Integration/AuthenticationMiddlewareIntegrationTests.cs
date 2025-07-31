using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using UserManagementAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using UserManagementAPI.Data;
using Microsoft.AspNetCore.Identity;
using UserManagementAPI.Entities;

namespace UserManagementAPI_Testing.Integration
{
    public class AuthenticationMiddlewareIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly TestLoggerProvider _loggerProvider;

        public AuthenticationMiddlewareIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _loggerProvider = new TestLoggerProvider();
            
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<ILoggerProvider>(_loggerProvider);
                });

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new[]
                    {
                        new KeyValuePair<string, string?>("Jwt:Key", "YourSuperSecretKeyThatIsAtLeast32CharactersLongForTesting!"),
                        new KeyValuePair<string, string?>("Jwt:Issuer", "TestIssuer"),
                        new KeyValuePair<string, string?>("Jwt:Audience", "TestAudience"),
                        new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", "Server=(localdb)\\mssqllocaldb;Database=TestUserManagementAPI;Trusted_Connection=true;MultipleActiveResultSets=true")
                    });
                });
            });

            _client = _factory.CreateClient();
        }

        [Theory]
        [InlineData("/api/auth/login")]
        [InlineData("/api/auth/register")]
        [InlineData("/swagger")]
        [InlineData("/swagger/v1/swagger.json")]
        [InlineData("/health")]
        [InlineData("/favicon.ico")]
        public async Task TokenAuthenticationMiddleware_ExcludedPaths_AllowAccess(string path)
        {
            // Arrange
            _loggerProvider.Clear();

            // Act
            var response = await _client.GetAsync(path);

            // Assert
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            
            // Should not log authentication warnings for excluded paths
            var logs = _loggerProvider.GetLogs();
            var authLogs = logs.Where(log => log.Contains("No token provided")).ToList();
            Assert.Empty(authLogs);
        }

        [Fact]
        public async Task TokenAuthenticationMiddleware_ProtectedPath_RequiresToken()
        {
            // Arrange
            _loggerProvider.Clear();

            // Act
            var response = await _client.GetAsync("/api/test");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            
            var logs = _loggerProvider.GetLogs();
            Assert.Contains(logs, log => log.Contains("No token provided"));
        }

        [Fact]
        public async Task TokenAuthenticationMiddleware_ValidTokenInHeader_AllowsAccess()
        {
            // Arrange
            _loggerProvider.Clear();
            var token = GenerateValidJwtToken();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/test");

            // Assert
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            
            var logs = _loggerProvider.GetLogs();
            Assert.Contains(logs, log => log.Contains("authenticated for request"));
        }

        [Fact]
        public async Task TokenAuthenticationMiddleware_ValidTokenInQuery_AllowsAccess()
        {
            // Arrange
            _loggerProvider.Clear();
            var token = GenerateValidJwtToken();

            // Act
            var response = await _client.GetAsync($"/api/test?token={token}");

            // Assert
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            
            var logs = _loggerProvider.GetLogs();
            Assert.Contains(logs, log => log.Contains("authenticated for request"));
        }

        [Fact]
        public async Task TokenAuthenticationMiddleware_MalformedToken_ReturnsUnauthorized()
        {
            // Arrange
            _loggerProvider.Clear();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "malformed.token.here");

            // Act
            var response = await _client.GetAsync("/api/test");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(content);
            Assert.Equal("Invalid or expired token", errorResponse.GetProperty("message").GetString());
            
            var logs = _loggerProvider.GetLogs();
            Assert.Contains(logs, log => log.Contains("Invalid token provided"));
        }

        [Fact]
        public async Task TokenAuthenticationMiddleware_ExpiredToken_ReturnsUnauthorized()
        {
            // Arrange
            _loggerProvider.Clear();
            var expiredToken = GenerateExpiredJwtToken();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expiredToken);

            // Act
            var response = await _client.GetAsync("/api/test");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(content);
            Assert.Equal("Invalid or expired token", errorResponse.GetProperty("message").GetString());
            
            var logs = _loggerProvider.GetLogs();
            Assert.Contains(logs, log => log.Contains("Invalid token provided"));
        }

        [Fact]
        public async Task TokenAuthenticationMiddleware_TokenWithWrongSignature_ReturnsUnauthorized()
        {
            // Arrange
            _loggerProvider.Clear();
            var tokenWithWrongSignature = GenerateTokenWithWrongSignature();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenWithWrongSignature);

            // Act
            var response = await _client.GetAsync("/api/test");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            
            var logs = _loggerProvider.GetLogs();
            Assert.Contains(logs, log => log.Contains("Invalid token provided"));
        }

        [Fact]
        public async Task TokenAuthenticationMiddleware_TokenWithWrongIssuer_ReturnsUnauthorized()
        {
            // Arrange
            _loggerProvider.Clear();
            var tokenWithWrongIssuer = GenerateTokenWithWrongIssuer();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenWithWrongIssuer);

            // Act
            var response = await _client.GetAsync("/api/test");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            
            var logs = _loggerProvider.GetLogs();
            Assert.Contains(logs, log => log.Contains("Invalid token provided"));
        }

        [Fact]
        public async Task TokenAuthenticationMiddleware_EmptyBearerToken_ReturnsUnauthorized()
        {
            // Arrange
            _loggerProvider.Clear();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "");

            // Act
            var response = await _client.GetAsync("/api/test");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            
            var logs = _loggerProvider.GetLogs();
            Assert.Contains(logs, log => log.Contains("No token provided"));
        }

        [Fact]
        public async Task TokenAuthenticationMiddleware_NonBearerAuthHeader_ReturnsUnauthorized()
        {
            // Arrange
            _loggerProvider.Clear();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "dGVzdDp0ZXN0");

            // Act
            var response = await _client.GetAsync("/api/test");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            
            var logs = _loggerProvider.GetLogs();
            Assert.Contains(logs, log => log.Contains("No token provided"));
        }

        [Fact]
        public async Task TokenAuthenticationMiddleware_ValidToken_SetsUserContext()
        {
            // Arrange
            _loggerProvider.Clear();
            var token = GenerateValidJwtToken();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act - Use the simple test controller that doesn't have database dependencies
            var response = await _client.GetAsync("/api/test");

            // Debug: Print response details
            var content = await response.Content.ReadAsStringAsync();
            var logs = _loggerProvider.GetLogs();
            
            // Print debug information
            System.Console.WriteLine($"Status Code: {response.StatusCode}");
            System.Console.WriteLine($"Content: {content}");
            System.Console.WriteLine($"Logs: {string.Join("\n", logs)}");

            // Assert
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            
            // Verify the authentication middleware logged the user authentication
            Assert.Contains(logs, log => log.Contains("User test-user-id authenticated"));
            
            // Also verify the response content shows authentication worked
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<JsonElement>(content);
                Assert.Equal("test-user-id", result.GetProperty("userId").GetString());
                Assert.Equal("testuser", result.GetProperty("userName").GetString());
                Assert.True(result.GetProperty("authenticated").GetBoolean());
            }
        }

        [Fact]
        public async Task TokenAuthenticationMiddleware_CaseInsensitivePaths_WorkCorrectly()
        {
            // Arrange & Act
            var loginResponse = await _client.GetAsync("/API/AUTH/LOGIN");
            var swaggerResponse = await _client.GetAsync("/SWAGGER");

            // Assert
            Assert.NotEqual(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
            Assert.NotEqual(HttpStatusCode.Unauthorized, swaggerResponse.StatusCode);
        }

        [Fact]
        public async Task TokenAuthenticationMiddleware_ErrorResponse_HasCorrectFormat()
        {
            // Arrange & Act
            var response = await _client.GetAsync("/api/test");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            
            var content = await response.Content.ReadAsStringAsync();
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(content);
            
            Assert.Equal("Unauthorized", errorResponse.GetProperty("error").GetString());
            Assert.Equal("Access token is required", errorResponse.GetProperty("message").GetString());
            Assert.True(errorResponse.TryGetProperty("timestamp", out _));
            Assert.True(errorResponse.TryGetProperty("traceId", out _));
        }

        #region Helper Methods

        private string GenerateValidJwtToken()
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("YourSuperSecretKeyThatIsAtLeast32CharactersLongForTesting!");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                    new Claim(ClaimTypes.Name, "testuser"),
                    new Claim(ClaimTypes.Email, "test@example.com")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateExpiredJwtToken()
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("YourSuperSecretKeyThatIsAtLeast32CharactersLongForTesting!");
            var expiredTime = DateTime.UtcNow.AddHours(-1);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                    new Claim(ClaimTypes.Name, "testuser"),
                    new Claim(ClaimTypes.Email, "test@example.com")
                }),
                NotBefore = expiredTime.AddMinutes(-5), // NotBefore should be before Expires
                Expires = expiredTime, // Expired token
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateTokenWithWrongSignature()
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var wrongKey = Encoding.UTF8.GetBytes("WrongSecretKeyThatIsAtLeast32CharactersLongForTesting!");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                    new Claim(ClaimTypes.Name, "testuser"),
                    new Claim(ClaimTypes.Email, "test@example.com")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(wrongKey), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateTokenWithWrongIssuer()
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("YourSuperSecretKeyThatIsAtLeast32CharactersLongForTesting!");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                    new Claim(ClaimTypes.Name, "testuser"),
                    new Claim(ClaimTypes.Email, "test@example.com")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = "WrongIssuer", // Wrong issuer
                Audience = "TestAudience",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        #endregion

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
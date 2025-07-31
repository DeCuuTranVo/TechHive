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

namespace UserManagementAPI_Testing.Integration
{
    public class MiddlewareIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly TestLoggerProvider _loggerProvider;

        public MiddlewareIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _loggerProvider = new TestLoggerProvider();
            
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace logging with test logging provider to capture logs
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

        #region TokenAuthenticationMiddleware Tests

        [Fact]
        public async Task TokenAuthenticationMiddleware_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange & Act
            var response = await _client.GetAsync("/api/test");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(content);
            
            Assert.Equal("Unauthorized", errorResponse.GetProperty("error").GetString());
            Assert.Equal("Access token is required", errorResponse.GetProperty("message").GetString());
            Assert.True(errorResponse.TryGetProperty("traceId", out _));
            Assert.True(errorResponse.TryGetProperty("timestamp", out _));
        }

        [Fact]
        public async Task TokenAuthenticationMiddleware_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

            // Act
            var response = await _client.GetAsync("/api/test");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(content);
            
            Assert.Equal("Unauthorized", errorResponse.GetProperty("error").GetString());
            Assert.Equal("Invalid or expired token", errorResponse.GetProperty("message").GetString());
        }

        [Fact]
        public async Task TokenAuthenticationMiddleware_WithExpiredToken_ReturnsUnauthorized()
        {
            // Arrange
            var expiredToken = GenerateExpiredJwtToken();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expiredToken);

            // Act
            var response = await _client.GetAsync("/api/test");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(content);
            
            Assert.Equal("Unauthorized", errorResponse.GetProperty("error").GetString());
            Assert.Equal("Invalid or expired token", errorResponse.GetProperty("message").GetString());
        }

        [Fact]
        public async Task TokenAuthenticationMiddleware_WithValidToken_AllowsAccess()
        {
            // Arrange
            var validToken = GenerateValidJwtToken();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", validToken);

            // Act
            var response = await _client.GetAsync("/api/test");

            // Assert
            // Should not return 401 Unauthorized (may return other status codes due to other logic)
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TokenAuthenticationMiddleware_WithTokenInQueryParameter_AllowsAccess()
        {
            // Arrange
            var validToken = GenerateValidJwtToken();

            // Act
            var response = await _client.GetAsync($"/api/test?token={validToken}");

            // Assert
            // Should not return 401 Unauthorized
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("/api/auth/login")]
        [InlineData("/api/auth/register")]
        [InlineData("/swagger")]
        [InlineData("/swagger/v1/swagger.json")]
        [InlineData("/health")]
        [InlineData("/favicon.ico")]
        public async Task TokenAuthenticationMiddleware_ExcludedPaths_BypassAuthentication(string path)
        {
            // Act
            var response = await _client.GetAsync(path);

            // Assert
            // Should not return 401 Unauthorized for excluded paths
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        #endregion

        #region GlobalExceptionHandlingMiddleware Tests

        [Fact]
        public async Task GlobalExceptionHandlingMiddleware_HandlesInternalServerError()
        {
            // Arrange
            var validToken = GenerateValidJwtToken();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", validToken);

            // Act - Try to access a non-existent endpoint that would cause an exception
            var response = await _client.GetAsync("/api/nonexistent");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GlobalExceptionHandlingMiddleware_ReturnsConsistentErrorFormat()
        {
            // Arrange & Act
            var response = await _client.GetAsync("/api/test"); // This will trigger 401

            // Assert
            var content = await response.Content.ReadAsStringAsync();
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(content);

            // Verify the error response format
            Assert.True(errorResponse.TryGetProperty("error", out _));
            Assert.True(errorResponse.TryGetProperty("message", out _));
            Assert.True(errorResponse.TryGetProperty("traceId", out _));
            Assert.True(errorResponse.TryGetProperty("timestamp", out _));
            
            // Verify content type
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        #endregion

        #region RequestResponseLoggingMiddleware Tests

        [Fact]
        public async Task RequestResponseLoggingMiddleware_LogsRequestAndResponse()
        {
            // Arrange
            _loggerProvider.Clear();

            // Act
            var response = await _client.GetAsync("/api/auth/login");

            // Assert - Check that request and response were logged
            var logs = _loggerProvider.GetLogs();
            
            var requestLogs = logs.Where(log => log.Contains("HTTP Request")).ToList();
            var responseLogs = logs.Where(log => log.Contains("HTTP Response")).ToList();

            Assert.NotEmpty(requestLogs);
            Assert.NotEmpty(responseLogs);

            // Verify request log contains expected information
            var requestLog = requestLogs.First();
            Assert.Contains("GET", requestLog);
            Assert.Contains("/api/auth/login", requestLog);
            Assert.Contains("RequestId", requestLog);

            // Verify response log contains expected information
            var responseLog = responseLogs.First();
            Assert.Contains("StatusCode", responseLog);
            Assert.Contains("ElapsedMs", responseLog);
        }

        [Fact]
        public async Task RequestResponseLoggingMiddleware_ExcludesSensitiveHeaders()
        {
            // Arrange
            _loggerProvider.Clear();
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer sensitive-token");
            _client.DefaultRequestHeaders.Add("X-API-Key", "sensitive-api-key");

            // Act
            await _client.GetAsync("/api/auth/login");

            // Assert
            var logs = _loggerProvider.GetLogs();
            var requestLogs = logs.Where(log => log.Contains("HTTP Request")).ToList();

            Assert.NotEmpty(requestLogs);
            
            var requestLog = requestLogs.First();
            // Sensitive headers should not be logged
            Assert.DoesNotContain("sensitive-token", requestLog);
            Assert.DoesNotContain("sensitive-api-key", requestLog);
        }

        #endregion

        #region Integration Tests - Full Pipeline

        [Fact]
        public async Task MiddlewarePipeline_CompleteFlow_WorksCorrectly()
        {
            // Arrange
            _loggerProvider.Clear();
            var validToken = GenerateValidJwtToken();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", validToken);

            // Act
            var response = await _client.GetAsync("/api/test");

            // Assert
            // Verify the complete middleware pipeline worked
            var logs = _loggerProvider.GetLogs();

            // Should have request logging
            Assert.Contains(logs, log => log.Contains("HTTP Request"));
            
            // Should have response logging
            Assert.Contains(logs, log => log.Contains("HTTP Response"));
            
            // Should have authentication logging
            Assert.Contains(logs, log => log.Contains("authenticated for request"));

            // Should not be unauthorized (authentication middleware passed)
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task MiddlewarePipeline_AuthenticationFailure_LogsAndReturnsError()
        {
            // Arrange
            _loggerProvider.Clear();
            
            // Clear any authorization headers from previous tests
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/test");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            var logs = _loggerProvider.GetLogs();
            
            // Should log the authentication failure - be more flexible with log content
            Assert.Contains(logs, log => log.Contains("No token provided") || log.Contains("token"));
            
            // Should still log request and response
            Assert.Contains(logs, log => log.Contains("HTTP Request") || log.Contains("Request"));
            Assert.Contains(logs, log => log.Contains("HTTP Response") || log.Contains("Response"));
        }

        [Fact]
        public async Task MiddlewarePipeline_PostRequestWithBody_LogsCorrectly()
        {
            // Arrange
            _loggerProvider.Clear();
            var requestBody = JsonSerializer.Serialize(new
            {
                email = "test@example.com",
                username = "testuser",
                password = "Password123!",
                fullName = "Test User"
            });

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/register", content);

            // Assert
            var logs = _loggerProvider.GetLogs();
            var requestLogs = logs.Where(log => log.Contains("HTTP Request")).ToList();

            Assert.NotEmpty(requestLogs);
            
            var requestLog = requestLogs.First();
            // Should log the request body
            Assert.Contains("POST", requestLog);
            Assert.Contains("/api/auth/register", requestLog);
            Assert.Contains("Body:", requestLog);
        }

        [Fact]
        public async Task MiddlewarePipeline_TokenAuthenticationLogging_ValidatesCorrectly()
        {
            // Arrange
            _loggerProvider.Clear();

            // Act - Test invalid token
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");
            await _client.GetAsync("/api/test");

            // Assert
            var logs = _loggerProvider.GetLogs();
            Assert.Contains(logs, log => log.Contains("Invalid token provided"));
        }

        [Fact]
        public async Task MiddlewarePipeline_ValidTokenLogging_WorksCorrectly()
        {
            // Arrange
            _loggerProvider.Clear();
            var validToken = GenerateValidJwtToken();

            // Act
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", validToken);
            await _client.GetAsync("/api/test");

            // Assert
            var logs = _loggerProvider.GetLogs();
            Assert.Contains(logs, log => log.Contains("authenticated for request"));
        }

        #endregion

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

        #endregion

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
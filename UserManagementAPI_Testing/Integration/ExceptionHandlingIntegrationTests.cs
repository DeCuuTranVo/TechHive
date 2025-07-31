using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using UserManagementAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using UserManagementAPI.Middleware;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace UserManagementAPI_Testing.Integration
{
    public class ExceptionHandlingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly TestLoggerProvider _loggerProvider;

        public ExceptionHandlingIntegrationTests(WebApplicationFactory<Program> factory)
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
                        new KeyValuePair<string, string?>("Jwt:Audience", "TestAudience")
                    });
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GlobalExceptionHandlingMiddleware_CatchesUnhandledExceptions()
        {
            // Arrange
            _loggerProvider.Clear();

            // Act - Request to endpoint that throws an exception (no token needed since /api/test is excluded)
            var response = await _client.GetAsync("/api/test/exception");

            // Debug output
            var content = await response.Content.ReadAsStringAsync();
            System.Console.WriteLine($"Status: {response.StatusCode}");
            System.Console.WriteLine($"Content: {content}");
            
            var logs = _loggerProvider.GetLogs();
            System.Console.WriteLine($"Logs: {string.Join("\n", logs)}");

            // Assert - The middleware should catch the exception and return a structured error response
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); // InvalidOperationException maps to Conflict
            
            Assert.NotNull(content);
            Assert.NotEmpty(content);
            
            // Verify JSON response format
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(content);
            Assert.True(errorResponse.TryGetProperty("traceId", out _));
            Assert.True(errorResponse.TryGetProperty("message", out _));
            Assert.True(errorResponse.TryGetProperty("timestamp", out _));
            Assert.Equal("Conflict", errorResponse.GetProperty("error").GetString());
        }

        [Fact]
        public async Task GlobalExceptionHandlingMiddleware_LogsExceptionDetails()
        {
            // Arrange
            _loggerProvider.Clear();

            // Act - Use endpoint that throws an exception (no token needed since /api/test is excluded)
            await _client.GetAsync("/api/test/exception");

            // Assert
            var logs = _loggerProvider.GetLogs();
            // Should log error details when exceptions occur
            var errorLogs = logs.Where(log => log.Contains("[Error]") || log.Contains("An unhandled exception occurred")).ToList();
            
            // Should have logged the exception
            Assert.NotEmpty(errorLogs);
        }

        [Fact]
        public async Task GlobalExceptionHandlingMiddleware_HandlesInvalidJsonRequest()
        {
            // Arrange
            var invalidJson = "{ invalid json content }";
            var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

            // Act - Use test endpoint that doesn't have database dependencies
            var response = await _client.PostAsync("/api/test/json", content);

            // Assert - Invalid JSON should be handled by ASP.NET Core model binding and return BadRequest
            // or it should be caught by GlobalExceptionHandlingMiddleware and return InternalServerError
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                       response.StatusCode == HttpStatusCode.InternalServerError);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseContent);
            
            // Should contain some error information
            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                // If it's handled by our middleware, it should have our error format
                var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                Assert.True(errorResponse.TryGetProperty("traceId", out _));
            }
        }

        [Fact]
        public async Task GlobalExceptionHandlingMiddleware_ReturnsCorrectContentType()
        {
            // Arrange & Act - Use endpoint that throws an exception (no token needed since /api/test is excluded)
            var response = await _client.GetAsync("/api/test/exception");

            // Assert
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task GlobalExceptionHandlingMiddleware_IncludesTraceIdInResponse()
        {
            // Arrange & Act - Use endpoint that throws an exception (no token needed since /api/test is excluded)
            var response = await _client.GetAsync("/api/test/exception");

            // Assert
            var content = await response.Content.ReadAsStringAsync();
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(content);
            
            Assert.True(errorResponse.TryGetProperty("traceId", out var traceId));
            Assert.False(string.IsNullOrEmpty(traceId.GetString()));
        }

        [Fact]
        public async Task GlobalExceptionHandlingMiddleware_HandlesLargeRequestBodies()
        {
            // Arrange
            var largeJson = JsonSerializer.Serialize(new
            {
                email = "test@example.com",
                username = "testuser",
                password = "Password123!",
                fullName = "Test User",
                description = new string('x', 10000) // Large description
            });
            
            var content = new StringContent(largeJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/register", content);

            // Assert
            // Should handle large requests without crashing
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                       response.StatusCode == HttpStatusCode.OK ||
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }

        public void Dispose()
        {
            _client?.Dispose();
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

        #endregion
    }
}
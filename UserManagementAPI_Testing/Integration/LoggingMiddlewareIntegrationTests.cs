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

namespace UserManagementAPI_Testing.Integration
{
    public class LoggingMiddlewareIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly TestLoggerProvider _loggerProvider;

        public LoggingMiddlewareIntegrationTests(WebApplicationFactory<Program> factory)
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
        public async Task RequestResponseLoggingMiddleware_LogsGetRequest()
        {
            // Arrange
            _loggerProvider.Clear();

            // Act
            await _client.GetAsync("/api/auth/login");

            // Assert
            var logs = _loggerProvider.GetLogs();
            var requestLogs = logs.Where(log => log.Contains("HTTP Request")).ToList();
            var responseLogs = logs.Where(log => log.Contains("HTTP Response")).ToList();

            Assert.NotEmpty(requestLogs);
            Assert.NotEmpty(responseLogs);

            var requestLog = requestLogs.First();
            Assert.Contains("GET", requestLog);
            Assert.Contains("/api/auth/login", requestLog);
            Assert.Contains("RequestId:", requestLog);
            Assert.Contains("Method:", requestLog);
            Assert.Contains("Path:", requestLog);
        }

        [Fact]
        public async Task RequestResponseLoggingMiddleware_LogsPostRequestWithBody()
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
            await _client.PostAsync("/api/auth/register", content);

            // Assert
            var logs = _loggerProvider.GetLogs();
            var requestLogs = logs.Where(log => log.Contains("HTTP Request")).ToList();

            Assert.NotEmpty(requestLogs);
            
            var requestLog = requestLogs.First();
            Assert.Contains("POST", requestLog);
            Assert.Contains("/api/auth/register", requestLog);
            Assert.Contains("Body:", requestLog);
            Assert.Contains("email", requestLog); // Should log the body content
        }

        [Fact]
        public async Task RequestResponseLoggingMiddleware_LogsResponseDetails()
        {
            // Arrange
            _loggerProvider.Clear();

            // Act
            var response = await _client.GetAsync("/api/auth/login");

            // Assert
            var logs = _loggerProvider.GetLogs();
            var responseLogs = logs.Where(log => log.Contains("HTTP Response")).ToList();

            Assert.NotEmpty(responseLogs);
            
            var responseLog = responseLogs.First();
            Assert.Contains("StatusCode:", responseLog);
            Assert.Contains("ElapsedMs:", responseLog);
            Assert.Contains("RequestId:", responseLog);
            
            // Should contain the actual status code
            Assert.Contains(((int)response.StatusCode).ToString(), responseLog);
        }

        [Fact]
        public async Task RequestResponseLoggingMiddleware_ExcludesSensitiveHeaders()
        {
            // Arrange
            _loggerProvider.Clear();
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");
            _client.DefaultRequestHeaders.Add("Cookie", "session=abc123");
            _client.DefaultRequestHeaders.Add("X-API-Key", "secret-key");
            _client.DefaultRequestHeaders.Add("X-Auth-Token", "auth-token");
            _client.DefaultRequestHeaders.Add("User-Agent", "TestClient/1.0");

            // Act
            await _client.GetAsync("/api/auth/login");

            // Assert
            var logs = _loggerProvider.GetLogs();
            var requestLogs = logs.Where(log => log.Contains("HTTP Request")).ToList();

            Assert.NotEmpty(requestLogs);
            
            var requestLog = requestLogs.First();
            
            // Sensitive headers should NOT be logged
            Assert.DoesNotContain("test-token", requestLog);
            Assert.DoesNotContain("abc123", requestLog);
            Assert.DoesNotContain("secret-key", requestLog);
            Assert.DoesNotContain("auth-token", requestLog);
            
            // Non-sensitive headers should be logged
            Assert.Contains("User-Agent", requestLog);
            Assert.Contains("TestClient/1.0", requestLog);
        }

        [Fact]
        public async Task RequestResponseLoggingMiddleware_HandlesQueryParameters()
        {
            // Arrange
            _loggerProvider.Clear();

            // Act
            await _client.GetAsync("/api/auth/login?returnUrl=/dashboard&test=value");

            // Assert
            var logs = _loggerProvider.GetLogs();
            var requestLogs = logs.Where(log => log.Contains("HTTP Request")).ToList();

            Assert.NotEmpty(requestLogs);
            
            var requestLog = requestLogs.First();
            Assert.Contains("Query:", requestLog);
            Assert.Contains("returnUrl", requestLog);
            Assert.Contains("test=value", requestLog);
        }

        [Fact]
        public async Task RequestResponseLoggingMiddleware_LogsEmptyBodyCorrectly()
        {
            // Arrange
            _loggerProvider.Clear();

            // Act
            await _client.GetAsync("/api/auth/login");

            // Assert
            var logs = _loggerProvider.GetLogs();
            var requestLogs = logs.Where(log => log.Contains("HTTP Request")).ToList();

            Assert.NotEmpty(requestLogs);
            
            var requestLog = requestLogs.First();
            Assert.Contains("Body: [Empty]", requestLog);
        }

        [Fact]
        public async Task RequestResponseLoggingMiddleware_MeasuresExecutionTime()
        {
            // Arrange
            _loggerProvider.Clear();

            // Act
            await _client.GetAsync("/api/auth/login");

            // Assert
            var logs = _loggerProvider.GetLogs();
            var responseLogs = logs.Where(log => log.Contains("HTTP Response")).ToList();

            Assert.NotEmpty(responseLogs);
            
            var responseLog = responseLogs.First();
            Assert.Contains("ElapsedMs:", responseLog);
            
            // Should have a numeric value for elapsed time
            var elapsedMatch = System.Text.RegularExpressions.Regex.Match(responseLog, @"ElapsedMs: (\d+)");
            Assert.True(elapsedMatch.Success);
            
            var elapsedMs = int.Parse(elapsedMatch.Groups[1].Value);
            Assert.True(elapsedMs >= 0);
        }

        [Fact]
        public async Task RequestResponseLoggingMiddleware_HandlesLargeRequestBody()
        {
            // Arrange
            _loggerProvider.Clear();
            var largeBody = JsonSerializer.Serialize(new
            {
                email = "test@example.com",
                description = new string('x', 2 * 1024 * 1024) // 2MB content
            });

            var content = new StringContent(largeBody, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/register", content);

            // Assert
            var logs = _loggerProvider.GetLogs();
            var requestLogs = logs.Where(log => log.Contains("HTTP Request")).ToList();

            // Should still log even with large body (may truncate or skip body)
            Assert.NotEmpty(requestLogs);
            
            var requestLog = requestLogs.First();
            Assert.Contains("POST", requestLog);
            Assert.Contains("/api/auth/register", requestLog);
        }

        [Fact]
        public async Task RequestResponseLoggingMiddleware_AssignsUniqueRequestIds()
        {
            // Arrange
            _loggerProvider.Clear();

            // Act - Make multiple concurrent requests
            var tasks = new[]
            {
                _client.GetAsync("/api/auth/login"),
                _client.GetAsync("/api/auth/login"),
                _client.GetAsync("/api/auth/login")
            };

            await Task.WhenAll(tasks);

            // Assert
            var logs = _loggerProvider.GetLogs();
            var requestLogs = logs.Where(log => log.Contains("HTTP Request")).ToList();

            Assert.True(requestLogs.Count >= 3);

            // Extract RequestIds and verify they're unique
            var requestIds = new List<string>();
            foreach (var log in requestLogs)
            {
                var match = System.Text.RegularExpressions.Regex.Match(log, @"RequestId: ([a-f0-9\-]+)");
                if (match.Success)
                {
                    requestIds.Add(match.Groups[1].Value);
                }
            }

            Assert.True(requestIds.Count >= 3);
            Assert.Equal(requestIds.Count, requestIds.Distinct().Count()); // All should be unique
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
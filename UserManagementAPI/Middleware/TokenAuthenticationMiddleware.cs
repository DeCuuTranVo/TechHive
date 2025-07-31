using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace UserManagementAPI.Middleware
{
    public class TokenAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenAuthenticationMiddleware> _logger;
        private readonly IConfiguration _configuration;

        public TokenAuthenticationMiddleware(RequestDelegate next, ILogger<TokenAuthenticationMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip authentication for certain paths
            if (IsExcludedPath(context.Request.Path))
            {
                await _next(context);
                return;
            }

            try
            {
                var token = ExtractTokenFromRequest(context.Request);

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("No token provided for request to {Path}", context.Request.Path);
                    await ReturnUnauthorizedResponse(context, "Access token is required");
                    return;
                }

                var principal = ValidateToken(token);
                if (principal == null)
                {
                    _logger.LogWarning("Invalid token provided for request to {Path}", context.Request.Path);
                    await ReturnUnauthorizedResponse(context, "Invalid or expired token");
                    return;
                }

                // Add user information to the context
                context.User = principal;
                context.Items["UserId"] = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                context.Items["UserName"] = principal.FindFirst(ClaimTypes.Name)?.Value;

                _logger.LogInformation("User {UserId} authenticated for request to {Path}", 
                    context.Items["UserId"], context.Request.Path);

                await _next(context);
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Security token exception for request to {Path}", context.Request.Path);
                await ReturnUnauthorizedResponse(context, "Invalid token format");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during authentication for request to {Path}", context.Request.Path);
                await ReturnUnauthorizedResponse(context, "Authentication failed");
            }
        }

        private static bool IsExcludedPath(PathString path)
        {
            var excludedPaths = new[]
            {
                "/api/auth/login",
                "/api/auth/register",
                "/api/test/exception", // Only exclude exception testing endpoint
                "/api/test/json",      // Only exclude JSON testing endpoint
                "/swagger",
                "/swagger/v1/swagger.json",
                "/health",
                "/favicon.ico"
            };

            return excludedPaths.Any(excludedPath => 
                path.StartsWithSegments(excludedPath, StringComparison.OrdinalIgnoreCase));
        }

        private static string? ExtractTokenFromRequest(HttpRequest request)
        {
            // Check Authorization header
            if (request.Headers.ContainsKey("Authorization"))
            {
                var authHeader = request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return authHeader.Substring("Bearer ".Length).Trim();
                }
            }

            // Check query parameter as fallback
            if (request.Query.ContainsKey("token"))
            {
                return request.Query["token"].FirstOrDefault();
            }

            return null;
        }

        private ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!");

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"] ?? "UserManagementAPI",
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"] ?? "UserManagementAPI",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                
                // Ensure it's a JWT token
                if (validatedToken is not JwtSecurityToken jwtToken || 
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogDebug("Token is not a valid JWT with HMAC SHA256");
                    return null;
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Token validation failed");
                return null;
            }
        }

        private static async Task ReturnUnauthorizedResponse(HttpContext context, string message)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Unauthorized",
                message = message,
                timestamp = DateTime.UtcNow,
                traceId = context.TraceIdentifier
            };

            var jsonResponse = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
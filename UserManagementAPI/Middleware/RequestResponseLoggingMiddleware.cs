using System.Diagnostics;
using System.Text;

namespace UserManagementAPI.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();

            // Log incoming request
            await LogRequestAsync(context, requestId);

            // Capture the original response body stream
            var originalResponseBodyStream = context.Response.Body;

            try
            {
                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                // Call the next middleware in the pipeline
                await _next(context);

                stopwatch.Stop();

                // Log outgoing response
                await LogResponseAsync(context, requestId, stopwatch.ElapsedMilliseconds, responseBodyStream);

                // Copy the captured response back to the original stream
                responseBodyStream.Position = 0;
                await responseBodyStream.CopyToAsync(originalResponseBodyStream);
            }
            finally
            {
                context.Response.Body = originalResponseBodyStream;
            }
        }

        private async Task LogRequestAsync(HttpContext context, string requestId)
        {
            try
            {
                var request = context.Request;
                var requestBody = string.Empty;

                // Read request body if it exists and is not too large
                if (request.ContentLength > 0 && request.ContentLength < 1024 * 1024) // 1MB limit
                {
                    request.EnableBuffering();
                    request.Body.Position = 0;
                    
                    using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                    requestBody = await reader.ReadToEndAsync();
                    request.Body.Position = 0;
                }

                _logger.LogInformation(
                    "HTTP Request | RequestId: {RequestId} | Method: {Method} | Path: {Path} | Query: {Query} | Headers: {Headers} | Body: {Body}",
                    requestId,
                    request.Method,
                    request.Path,
                    request.QueryString,
                    GetHeaders(request.Headers),
                    string.IsNullOrEmpty(requestBody) ? "[Empty]" : requestBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging request for RequestId: {RequestId}", requestId);
            }
        }

        private async Task LogResponseAsync(HttpContext context, string requestId, long elapsedMs, MemoryStream responseBodyStream)
        {
            try
            {
                var response = context.Response;
                var responseBody = string.Empty;

                // Read response body if it's not too large
                if (responseBodyStream.Length > 0 && responseBodyStream.Length < 1024 * 1024) // 1MB limit
                {
                    responseBodyStream.Position = 0;
                    using var reader = new StreamReader(responseBodyStream, Encoding.UTF8, leaveOpen: true);
                    responseBody = await reader.ReadToEndAsync();
                }

                _logger.LogInformation(
                    "HTTP Response | RequestId: {RequestId} | StatusCode: {StatusCode} | ContentType: {ContentType} | ElapsedMs: {ElapsedMs} | Headers: {Headers} | Body: {Body}",
                    requestId,
                    response.StatusCode,
                    response.ContentType ?? "[Not Set]",
                    elapsedMs,
                    GetHeaders(response.Headers),
                    string.IsNullOrEmpty(responseBody) ? "[Empty]" : responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging response for RequestId: {RequestId}", requestId);
            }
        }

        private static string GetHeaders(IHeaderDictionary headers)
        {
            var headerStrings = headers
                .Where(h => !IsSensitiveHeader(h.Key))
                .Select(h => $"{h.Key}: {string.Join(", ", h.Value)}")
                .ToArray();

            return headerStrings.Length > 0 ? string.Join(" | ", headerStrings) : "[No Headers]";
        }

        private static bool IsSensitiveHeader(string headerName)
        {
            var sensitiveHeaders = new[] { "Authorization", "Cookie", "Set-Cookie", "X-API-Key", "X-Auth-Token" };
            return sensitiveHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase);
        }
    }
}
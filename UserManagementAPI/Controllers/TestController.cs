using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var currentUserId = HttpContext.Items["UserId"]?.ToString();
            var currentUserName = HttpContext.Items["UserName"]?.ToString();
            
            return Ok(new { 
                Message = "Authentication successful", 
                UserId = currentUserId,
                UserName = currentUserName,
                Authenticated = !string.IsNullOrEmpty(currentUserId)
            });
        }

        [HttpGet("exception")]
        public IActionResult ThrowException()
        {
            throw new InvalidOperationException("Test exception for middleware testing");
        }

        [HttpPost("json")]
        public IActionResult TestJson([FromBody] object data)
        {
            return Ok(new { Message = "JSON received successfully", Data = data });
        }
    }
}
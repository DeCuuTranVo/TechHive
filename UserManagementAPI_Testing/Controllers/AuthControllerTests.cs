using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using UserManagementAPI.Controllers;
using UserManagementAPI.Services;
using Xunit;

namespace UserManagementAPI_Testing.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _controller = new AuthController(_mockAuthService.Object, _mockLogger.Object);
        }

        #region Login Tests

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var authResult = AuthResult.Success("mock-token", "user-id", "test@example.com", "testuser");
            _mockAuthService.Setup(x => x.LoginAsync(loginRequest.Email, loginRequest.Password))
                .ReturnsAsync(authResult);

            // Act
            var result = await _controller.Login(loginRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<LoginResponse>(okResult.Value);
            Assert.Equal("mock-token", response.Token);
            Assert.Equal("user-id", response.UserId);
            Assert.Equal("test@example.com", response.Email);
            Assert.Equal("testuser", response.Username);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Email = "test@example.com",
                Password = "wrongpassword"
            };

            var authResult = AuthResult.Failure("Invalid email or password");
            _mockAuthService.Setup(x => x.LoginAsync(loginRequest.Email, loginRequest.Password))
                .ReturnsAsync(authResult);

            // Act
            var result = await _controller.Login(loginRequest);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            var response = unauthorizedResult.Value;
            Assert.NotNull(response);
        }

        [Fact]
        public async Task Login_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var loginRequest = new LoginRequest { Email = "", Password = "" };
            _controller.ModelState.AddModelError("Email", "Email is required");

            // Act
            var result = await _controller.Login(loginRequest);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Login_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Login(loginRequest);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region Register Tests

        [Fact]
        public async Task Register_WithValidData_ReturnsOkWithToken()
        {
            // Arrange
            var registerRequest = new RegisterRequest
            {
                Email = "newuser@example.com",
                Username = "newuser",
                Password = "Password123!",
                FullName = "New User"
            };

            var authResult = AuthResult.Success("mock-token", "new-user-id", "newuser@example.com", "newuser");
            _mockAuthService.Setup(x => x.RegisterAsync(
                registerRequest.Email,
                registerRequest.Username,
                registerRequest.Password,
                registerRequest.FullName))
                .ReturnsAsync(authResult);

            // Act
            var result = await _controller.Register(registerRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<LoginResponse>(okResult.Value);
            Assert.Equal("mock-token", response.Token);
            Assert.Equal("new-user-id", response.UserId);
            Assert.Equal("newuser@example.com", response.Email);
            Assert.Equal("newuser", response.Username);
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
        {
            // Arrange
            var registerRequest = new RegisterRequest
            {
                Email = "existing@example.com",
                Username = "newuser",
                Password = "Password123!",
                FullName = "New User"
            };

            var authResult = AuthResult.Failure("Email is already registered");
            _mockAuthService.Setup(x => x.RegisterAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(authResult);

            // Act
            var result = await _controller.Register(registerRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = badRequestResult.Value;
            Assert.NotNull(response);
        }

        [Fact]
        public async Task Register_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var registerRequest = new RegisterRequest();
            _controller.ModelState.AddModelError("Email", "Email is required");

            // Act
            var result = await _controller.Register(registerRequest);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Register_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var registerRequest = new RegisterRequest
            {
                Email = "test@example.com",
                Username = "testuser",
                Password = "Password123!",
                FullName = "Test User"
            };

            _mockAuthService.Setup(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Register(registerRequest);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region ValidateToken Tests

        [Fact]
        public async Task ValidateToken_WithValidToken_ReturnsOk()
        {
            // Arrange
            var validateRequest = new ValidateTokenRequest { Token = "valid-token" };
            _mockAuthService.Setup(x => x.ValidateTokenAsync("valid-token")).ReturnsAsync(true);

            // Act
            var result = await _controller.ValidateToken(validateRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);
        }

        [Fact]
        public async Task ValidateToken_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var validateRequest = new ValidateTokenRequest { Token = "invalid-token" };
            _mockAuthService.Setup(x => x.ValidateTokenAsync("invalid-token")).ReturnsAsync(false);

            // Act
            var result = await _controller.ValidateToken(validateRequest);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = unauthorizedResult.Value;
            Assert.NotNull(response);
        }

        [Fact]
        public async Task ValidateToken_WithEmptyToken_ReturnsBadRequest()
        {
            // Arrange
            var validateRequest = new ValidateTokenRequest { Token = "" };

            // Act
            var result = await _controller.ValidateToken(validateRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            Assert.NotNull(response);
        }

        [Fact]
        public async Task ValidateToken_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var validateRequest = new ValidateTokenRequest { Token = "some-token" };
            _mockAuthService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.ValidateToken(validateRequest);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion
    }
}
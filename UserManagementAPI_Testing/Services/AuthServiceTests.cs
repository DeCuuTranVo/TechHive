using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using UserManagementAPI.Entities;
using UserManagementAPI.Services;
using Xunit;

namespace UserManagementAPI_Testing.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<SignInManager<AppUser>> _mockSignInManager;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            // Setup UserManager mock
            var userStore = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(userStore.Object, null, null, null, null, null, null, null, null);

            // Setup SignInManager mock
            var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
            _mockSignInManager = new Mock<SignInManager<AppUser>>(_mockUserManager.Object, contextAccessor.Object, claimsFactory.Object, null, null, null, null);

            // Setup Configuration mock
            _mockConfiguration = new Mock<IConfiguration>();
            SetupConfiguration();

            _mockLogger = new Mock<ILogger<AuthService>>();

            _authService = new AuthService(
                _mockUserManager.Object,
                _mockSignInManager.Object,
                _mockConfiguration.Object,
                _mockLogger.Object);
        }

        private void SetupConfiguration()
        {
            _mockConfiguration.Setup(x => x["Jwt:Key"]).Returns("YourSuperSecretKeyThatIsAtLeast32CharactersLong!");
            _mockConfiguration.Setup(x => x["Jwt:Issuer"]).Returns("UserManagementAPI");
            _mockConfiguration.Setup(x => x["Jwt:Audience"]).Returns("UserManagementAPI");
            _mockConfiguration.Setup(x => x["Jwt:ExpiryInHours"]).Returns("24");
        }

        #region LoginAsync Tests

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsSuccessResult()
        {
            // Arrange
            var email = "test@example.com";
            var password = "Password123!";
            var user = new AppUser
            {
                Id = "user-id",
                Email = email,
                UserName = "testuser",
                FullName = "Test User"
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
            _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(user, password, true))
                .ReturnsAsync(SignInResult.Success);

            // Act
            var result = await _authService.LoginAsync(email, password);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Token);
            Assert.Equal(user.Id, result.UserId);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.UserName, result.Username);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidEmail_ReturnsFailureResult()
        {
            // Arrange
            var email = "nonexistent@example.com";
            var password = "Password123!";

            _mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync((AppUser?)null);

            // Act
            var result = await _authService.LoginAsync(email, password);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid email or password", result.ErrorMessage);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ReturnsFailureResult()
        {
            // Arrange
            var email = "test@example.com";
            var password = "wrongpassword";
            var user = new AppUser
            {
                Id = "user-id",
                Email = email,
                UserName = "testuser"
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
            _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(user, password, true))
                .ReturnsAsync(SignInResult.Failed);

            // Act
            var result = await _authService.LoginAsync(email, password);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid email or password", result.ErrorMessage);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task LoginAsync_WithLockedOutUser_ReturnsFailureResult()
        {
            // Arrange
            var email = "test@example.com";
            var password = "Password123!";
            var user = new AppUser
            {
                Id = "user-id",
                Email = email,
                UserName = "testuser"
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
            _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(user, password, true))
                .ReturnsAsync(SignInResult.LockedOut);

            // Act
            var result = await _authService.LoginAsync(email, password);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Account is locked out", result.ErrorMessage);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task LoginAsync_WhenExceptionThrown_ReturnsFailureResult()
        {
            // Arrange
            var email = "test@example.com";
            var password = "Password123!";

            _mockUserManager.Setup(x => x.FindByEmailAsync(email))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _authService.LoginAsync(email, password);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("An error occurred during login", result.ErrorMessage);
            Assert.Null(result.Token);
        }

        #endregion

        #region RegisterAsync Tests

        [Fact]
        public async Task RegisterAsync_WithValidData_ReturnsSuccessResult()
        {
            // Arrange
            var email = "newuser@example.com";
            var username = "newuser";
            var password = "Password123!";
            var fullName = "New User";

            _mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync((AppUser?)null);
            _mockUserManager.Setup(x => x.FindByNameAsync(username)).ReturnsAsync((AppUser?)null);
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), password))
                .ReturnsAsync(IdentityResult.Success)
                .Callback<AppUser, string>((user, pwd) =>
                {
                    user.Id = "new-user-id";
                });

            // Act
            var result = await _authService.RegisterAsync(email, username, password, fullName);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Token);
            Assert.Equal("new-user-id", result.UserId);
            Assert.Equal(email, result.Email);
            Assert.Equal(username, result.Username);
        }

        [Fact]
        public async Task RegisterAsync_WithDuplicateEmail_ReturnsFailureResult()
        {
            // Arrange
            var email = "existing@example.com";
            var username = "newuser";
            var password = "Password123!";
            var fullName = "New User";

            var existingUser = new AppUser { Email = email };
            _mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(existingUser);

            // Act
            var result = await _authService.RegisterAsync(email, username, password, fullName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Email is already registered", result.ErrorMessage);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task RegisterAsync_WithDuplicateUsername_ReturnsFailureResult()
        {
            // Arrange
            var email = "newuser@example.com";
            var username = "existinguser";
            var password = "Password123!";
            var fullName = "New User";

            var existingUser = new AppUser { UserName = username };
            _mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync((AppUser?)null);
            _mockUserManager.Setup(x => x.FindByNameAsync(username)).ReturnsAsync(existingUser);

            // Act
            var result = await _authService.RegisterAsync(email, username, password, fullName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Username is already taken", result.ErrorMessage);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task RegisterAsync_WithCreateFailure_ReturnsFailureResult()
        {
            // Arrange
            var email = "newuser@example.com";
            var username = "newuser";
            var password = "weak";
            var fullName = "New User";

            var errors = new[]
            {
                new IdentityError { Description = "Password too weak" }
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync((AppUser?)null);
            _mockUserManager.Setup(x => x.FindByNameAsync(username)).ReturnsAsync((AppUser?)null);
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), password))
                .ReturnsAsync(IdentityResult.Failed(errors));

            // Act
            var result = await _authService.RegisterAsync(email, username, password, fullName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Password too weak", result.ErrorMessage!);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task RegisterAsync_WhenExceptionThrown_ReturnsFailureResult()
        {
            // Arrange
            var email = "newuser@example.com";
            var username = "newuser";
            var password = "Password123!";
            var fullName = "New User";

            _mockUserManager.Setup(x => x.FindByEmailAsync(email))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _authService.RegisterAsync(email, username, password, fullName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("An error occurred during registration", result.ErrorMessage);
            Assert.Null(result.Token);
        }

        #endregion

        #region ValidateTokenAsync Tests

        [Fact]
        public async Task ValidateTokenAsync_WithValidToken_ReturnsTrue()
        {
            // Arrange
            var user = new AppUser
            {
                Id = "user-id",
                UserName = "testuser",
                Email = "test@example.com",
                FullName = "Test User"
            };

            // Generate a real token using the service's method
            var authResult = AuthResult.Success("token", user.Id, user.Email, user.UserName);
            
            _mockUserManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(user, "Password123!", true))
                .ReturnsAsync(SignInResult.Success);

            var loginResult = await _authService.LoginAsync(user.Email, "Password123!");
            var token = loginResult.Token!;

            // Act
            var isValid = await _authService.ValidateTokenAsync(token);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithInvalidToken_ReturnsFalse()
        {
            // Arrange
            var invalidToken = "invalid-token";

            // Act
            var isValid = await _authService.ValidateTokenAsync(invalidToken);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithExpiredToken_ReturnsFalse()
        {
            // Arrange
            var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

            // Act
            var isValid = await _authService.ValidateTokenAsync(expiredToken);

            // Assert
            Assert.False(isValid);
        }

        #endregion
    }
}
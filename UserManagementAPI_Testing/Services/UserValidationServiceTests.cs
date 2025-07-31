using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using UserManagementAPI.Entities;
using UserManagementAPI.Models;
using UserManagementAPI.Services;
using Xunit;

namespace UserManagementAPI_Testing.Services
{
    public class UserValidationServiceTests
    {
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<ILogger<UserValidationService>> _mockLogger;
        private readonly UserValidationService _validationService;

        public UserValidationServiceTests()
        {
            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
            _mockLogger = new Mock<ILogger<UserValidationService>>();
            _validationService = new UserValidationService(_mockUserManager.Object, _mockLogger.Object);
        }

        #region ValidateCreateUserAsync Tests

        [Fact]
        public async Task ValidateCreateUserAsync_WithValidData_ReturnsValidResult()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                Email = "test@example.com",
                UserName = "testuser",
                FullName = "Test User",
                Password = "Password123!",
                Description = "Test description"
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(createUserDto.Email)).ReturnsAsync((AppUser?)null);
            _mockUserManager.Setup(x => x.FindByNameAsync(createUserDto.UserName)).ReturnsAsync((AppUser?)null);

            // Act
            var result = await _validationService.ValidateCreateUserAsync(createUserDto);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateCreateUserAsync_WithDuplicateEmail_ReturnsInvalidResult()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                Email = "existing@example.com",
                UserName = "testuser",
                FullName = "Test User",
                Password = "Password123!"
            };

            var existingUser = new AppUser { Email = createUserDto.Email };
            _mockUserManager.Setup(x => x.FindByEmailAsync(createUserDto.Email)).ReturnsAsync(existingUser);
            _mockUserManager.Setup(x => x.FindByNameAsync(createUserDto.UserName)).ReturnsAsync((AppUser?)null);

            // Act
            var result = await _validationService.ValidateCreateUserAsync(createUserDto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == "Email" && e.Message == "Email is already in use");
        }

        [Fact]
        public async Task ValidateCreateUserAsync_WithDuplicateUsername_ReturnsInvalidResult()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                Email = "test@example.com",
                UserName = "existinguser",
                FullName = "Test User",
                Password = "Password123!"
            };

            var existingUser = new AppUser { UserName = createUserDto.UserName };
            _mockUserManager.Setup(x => x.FindByEmailAsync(createUserDto.Email)).ReturnsAsync((AppUser?)null);
            _mockUserManager.Setup(x => x.FindByNameAsync(createUserDto.UserName)).ReturnsAsync(existingUser);

            // Act
            var result = await _validationService.ValidateCreateUserAsync(createUserDto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == "UserName" && e.Message == "Username is already in use");
        }

        [Fact]
        public async Task ValidateCreateUserAsync_WithInvalidEmail_ReturnsInvalidResult()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                Email = "invalid-email",
                UserName = "testuser",
                FullName = "Test User",
                Password = "Password123!"
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(createUserDto.Email)).ReturnsAsync((AppUser?)null);
            _mockUserManager.Setup(x => x.FindByNameAsync(createUserDto.UserName)).ReturnsAsync((AppUser?)null);

            // Act
            var result = await _validationService.ValidateCreateUserAsync(createUserDto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == "Email");
        }

        [Fact]
        public async Task ValidateCreateUserAsync_WithWeakPassword_ReturnsInvalidResult()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                Email = "test@example.com",
                UserName = "testuser",
                FullName = "Test User",
                Password = "weak"
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(createUserDto.Email)).ReturnsAsync((AppUser?)null);
            _mockUserManager.Setup(x => x.FindByNameAsync(createUserDto.UserName)).ReturnsAsync((AppUser?)null);

            // Act
            var result = await _validationService.ValidateCreateUserAsync(createUserDto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == "Password");
        }

        #endregion

        #region ValidateUpdateUserAsync Tests

        [Fact]
        public async Task ValidateUpdateUserAsync_WithValidData_ReturnsValidResult()
        {
            // Arrange
            var userId = "user-id";
            var updateUserDto = new UpdateUserDto
            {
                Email = "test@example.com",
                UserName = "testuser",
                FullName = "Test User",
                Description = "Updated description"
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(updateUserDto.Email)).ReturnsAsync((AppUser?)null);
            _mockUserManager.Setup(x => x.FindByNameAsync(updateUserDto.UserName)).ReturnsAsync((AppUser?)null);

            // Act
            var result = await _validationService.ValidateUpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateUpdateUserAsync_WithDuplicateEmailFromOtherUser_ReturnsInvalidResult()
        {
            // Arrange
            var userId = "user-id";
            var updateUserDto = new UpdateUserDto
            {
                Email = "existing@example.com",
                UserName = "testuser",
                FullName = "Test User"
            };

            var existingUser = new AppUser { Id = "other-user-id", Email = updateUserDto.Email };
            _mockUserManager.Setup(x => x.FindByEmailAsync(updateUserDto.Email)).ReturnsAsync(existingUser);

            // Act
            var result = await _validationService.ValidateUpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == "Email" && e.Message == "Email is already in use");
        }

        [Fact]
        public async Task ValidateUpdateUserAsync_WithSameUserEmail_ReturnsValidResult()
        {
            // Arrange
            var userId = "user-id";
            var updateUserDto = new UpdateUserDto
            {
                Email = "test@example.com",
                UserName = "testuser",
                FullName = "Test User"
            };

            var existingUser = new AppUser { Id = userId, Email = updateUserDto.Email };
            _mockUserManager.Setup(x => x.FindByEmailAsync(updateUserDto.Email)).ReturnsAsync(existingUser);
            _mockUserManager.Setup(x => x.FindByNameAsync(updateUserDto.UserName)).ReturnsAsync((AppUser?)null);

            // Act
            var result = await _validationService.ValidateUpdateUserAsync(userId, updateUserDto);

            // Assert
            Assert.True(result.IsValid);
        }

        #endregion

        #region ValidateUserInput Tests

        [Fact]
        public void ValidateUserInput_WithValidInput_ReturnsValidResult()
        {
            // Arrange
            var input = "validinput";
            var fieldName = "TestField";
            var minLength = 5;
            var maxLength = 20;

            // Act
            var result = _validationService.ValidateUserInput(input, fieldName, minLength, maxLength);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateUserInput_WithNullInput_ReturnsInvalidResult()
        {
            // Arrange
            string? input = null;
            var fieldName = "TestField";
            var minLength = 5;
            var maxLength = 20;

            // Act
            var result = _validationService.ValidateUserInput(input, fieldName, minLength, maxLength);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == fieldName && e.Message == "TestField is required");
        }

        [Fact]
        public void ValidateUserInput_WithTooShortInput_ReturnsInvalidResult()
        {
            // Arrange
            var input = "abc";
            var fieldName = "TestField";
            var minLength = 5;
            var maxLength = 20;

            // Act
            var result = _validationService.ValidateUserInput(input, fieldName, minLength, maxLength);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == fieldName && e.Message.Contains("at least 5 characters"));
        }

        [Fact]
        public void ValidateUserInput_WithTooLongInput_ReturnsInvalidResult()
        {
            // Arrange
            var input = new string('a', 25);
            var fieldName = "TestField";
            var minLength = 5;
            var maxLength = 20;

            // Act
            var result = _validationService.ValidateUserInput(input, fieldName, minLength, maxLength);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == fieldName && e.Message.Contains("cannot exceed 20 characters"));
        }

        [Fact]
        public void ValidateUserInput_WithInvalidPattern_ReturnsInvalidResult()
        {
            // Arrange
            var input = "invalid@input";
            var fieldName = "TestField";
            var minLength = 5;
            var maxLength = 20;
            var pattern = @"^[a-zA-Z0-9]+$"; // Only alphanumeric

            // Act
            var result = _validationService.ValidateUserInput(input, fieldName, minLength, maxLength, pattern);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == fieldName && e.Message.Contains("invalid characters"));
        }

        [Fact]
        public void ValidateUserInput_WithValidPattern_ReturnsValidResult()
        {
            // Arrange
            var input = "validinput123";
            var fieldName = "TestField";
            var minLength = 5;
            var maxLength = 20;
            var pattern = @"^[a-zA-Z0-9]+$"; // Only alphanumeric

            // Act
            var result = _validationService.ValidateUserInput(input, fieldName, minLength, maxLength, pattern);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        #endregion
    }
}
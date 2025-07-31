using System.ComponentModel.DataAnnotations;
using UserManagementAPI.Models;
using Xunit;

namespace UserManagementAPI_Testing.Models
{
    public class CreateUserDtoTests
    {
        [Fact]
        public void CreateUserDto_WithValidData_PassesValidation()
        {
            // Arrange
            var dto = new CreateUserDto
            {
                Email = "test@example.com",
                UserName = "testuser",
                FullName = "Test User",
                Password = "Password123!",
                Description = "Test description"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void CreateUserDto_WithInvalidEmail_FailsValidation()
        {
            // Arrange
            var dto = new CreateUserDto
            {
                Email = "invalid-email",
                UserName = "testuser",
                FullName = "Test User",
                Password = "Password123!"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("Email"));
        }

        [Fact]
        public void CreateUserDto_WithEmptyEmail_FailsValidation()
        {
            // Arrange
            var dto = new CreateUserDto
            {
                Email = "",
                UserName = "testuser",
                FullName = "Test User",
                Password = "Password123!"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("Email"));
        }

        [Fact]
        public void CreateUserDto_WithInvalidUserName_FailsValidation()
        {
            // Arrange
            var dto = new CreateUserDto
            {
                Email = "test@example.com",
                UserName = "a", // Too short
                FullName = "Test User",
                Password = "Password123!"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("UserName"));
        }

        [Fact]
        public void CreateUserDto_WithSpecialCharactersInUserName_FailsValidation()
        {
            // Arrange
            var dto = new CreateUserDto
            {
                Email = "test@example.com",
                UserName = "user@name", // Contains invalid character
                FullName = "Test User",
                Password = "Password123!"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("UserName"));
        }

        [Fact]
        public void CreateUserDto_WithWeakPassword_FailsValidation()
        {
            // Arrange
            var dto = new CreateUserDto
            {
                Email = "test@example.com",
                UserName = "testuser",
                FullName = "Test User",
                Password = "weak" // Too weak
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("Password"));
        }

        [Fact]
        public void CreateUserDto_WithInvalidFullName_FailsValidation()
        {
            // Arrange
            var dto = new CreateUserDto
            {
                Email = "test@example.com",
                UserName = "testuser",
                FullName = "Test123", // Contains numbers
                Password = "Password123!"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("FullName"));
        }

        [Fact]
        public void CreateUserDto_WithTooLongDescription_FailsValidation()
        {
            // Arrange
            var dto = new CreateUserDto
            {
                Email = "test@example.com",
                UserName = "testuser",
                FullName = "Test User",
                Password = "Password123!",
                Description = new string('a', 501) // Too long
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("Description"));
        }

        private static IList<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var ctx = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, ctx, validationResults, true);
            return validationResults;
        }
    }
}
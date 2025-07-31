using System.ComponentModel.DataAnnotations;
using UserManagementAPI.Models;
using Xunit;

namespace UserManagementAPI_Testing.Models
{
    public class UpdateUserDtoTests
    {
        [Fact]
        public void UpdateUserDto_WithValidData_PassesValidation()
        {
            // Arrange
            var dto = new UpdateUserDto
            {
                Email = "test@example.com",
                UserName = "testuser",
                FullName = "Test User",
                Description = "Test description"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void UpdateUserDto_WithInvalidEmail_FailsValidation()
        {
            // Arrange
            var dto = new UpdateUserDto
            {
                Email = "invalid-email",
                UserName = "testuser",
                FullName = "Test User"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("Email"));
        }

        [Fact]
        public void UpdateUserDto_WithEmptyEmail_FailsValidation()
        {
            // Arrange
            var dto = new UpdateUserDto
            {
                Email = "",
                UserName = "testuser",
                FullName = "Test User"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("Email"));
        }

        [Fact]
        public void UpdateUserDto_WithInvalidUserName_FailsValidation()
        {
            // Arrange
            var dto = new UpdateUserDto
            {
                Email = "test@example.com",
                UserName = "a", // Too short
                FullName = "Test User"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("UserName"));
        }

        [Fact]
        public void UpdateUserDto_WithSpecialCharactersInUserName_FailsValidation()
        {
            // Arrange
            var dto = new UpdateUserDto
            {
                Email = "test@example.com",
                UserName = "user@name", // Contains invalid character
                FullName = "Test User"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("UserName"));
        }

        [Fact]
        public void UpdateUserDto_WithInvalidFullName_FailsValidation()
        {
            // Arrange
            var dto = new UpdateUserDto
            {
                Email = "test@example.com",
                UserName = "testuser",
                FullName = "Test123" // Contains numbers
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("FullName"));
        }

        [Fact]
        public void UpdateUserDto_WithTooLongDescription_FailsValidation()
        {
            // Arrange
            var dto = new UpdateUserDto
            {
                Email = "test@example.com",
                UserName = "testuser",
                FullName = "Test User",
                Description = new string('a', 501) // Too long
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("Description"));
        }

        [Fact]
        public void UpdateUserDto_WithEmptyDescription_PassesValidation()
        {
            // Arrange
            var dto = new UpdateUserDto
            {
                Email = "test@example.com",
                UserName = "testuser",
                FullName = "Test User",
                Description = ""
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.Empty(validationResults);
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
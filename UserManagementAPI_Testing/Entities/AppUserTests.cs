using System.ComponentModel.DataAnnotations;
using UserManagementAPI.Entities;
using Xunit;

namespace UserManagementAPI_Testing.Entities
{
    public class AppUserTests
    {
        [Fact]
        public void AppUser_WithValidData_PassesValidation()
        {
            // Arrange
            var appUser = new AppUser
            {
                FullName = "John Doe",
                Description = "Test user description",
                UserName = "johndoe",
                Email = "john.doe@example.com"
            };

            // Act
            var validationResults = ValidateModel(appUser);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void AppUser_WithEmptyFullName_FailsValidation()
        {
            // Arrange
            var appUser = new AppUser
            {
                FullName = "",
                Description = "Test user description",
                UserName = "johndoe",
                Email = "john.doe@example.com"
            };

            // Act
            var validationResults = ValidateModel(appUser);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("FullName"));
        }

        [Fact]
        public void AppUser_WithNullFullName_FailsValidation()
        {
            // Arrange
            var appUser = new AppUser
            {
                FullName = null!,
                Description = "Test user description",
                UserName = "johndoe",
                Email = "john.doe@example.com"
            };

            // Act
            var validationResults = ValidateModel(appUser);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("FullName"));
        }

        [Fact]
        public void AppUser_WithTooShortFullName_FailsValidation()
        {
            // Arrange
            var appUser = new AppUser
            {
                FullName = "A", // Only 1 character, minimum is 2
                Description = "Test user description",
                UserName = "johndoe",
                Email = "john.doe@example.com"
            };

            // Act
            var validationResults = ValidateModel(appUser);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("FullName"));
        }

        [Fact]
        public void AppUser_WithTooLongFullName_FailsValidation()
        {
            // Arrange
            var appUser = new AppUser
            {
                FullName = new string('A', 101), // 101 characters, maximum is 100
                Description = "Test user description",
                UserName = "johndoe",
                Email = "john.doe@example.com"
            };

            // Act
            var validationResults = ValidateModel(appUser);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("FullName"));
        }

        [Fact]
        public void AppUser_WithInvalidCharactersInFullName_FailsValidation()
        {
            // Arrange
            var appUser = new AppUser
            {
                FullName = "John123", // Contains numbers, which are not allowed
                Description = "Test user description",
                UserName = "johndoe",
                Email = "john.doe@example.com"
            };

            // Act
            var validationResults = ValidateModel(appUser);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("FullName"));
        }

        [Fact]
        public void AppUser_WithSpecialSymbolsInFullName_FailsValidation()
        {
            // Arrange
            var appUser = new AppUser
            {
                FullName = "John@Doe", // Contains @, which is not allowed
                Description = "Test user description",
                UserName = "johndoe",
                Email = "john.doe@example.com"
            };

            // Act
            var validationResults = ValidateModel(appUser);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("FullName"));
        }

        [Fact]
        public void AppUser_WithValidSpecialCharactersInFullName_PassesValidation()
        {
            // Arrange - Test allowed special characters: spaces, dots, apostrophes, hyphens
            var appUser = new AppUser
            {
                FullName = "Mary-Jane O'Connor Jr.",
                Description = "Test user description",
                UserName = "maryjane",
                Email = "mary.jane@example.com"
            };

            // Act
            var validationResults = ValidateModel(appUser);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void AppUser_WithMinimumValidFullName_PassesValidation()
        {
            // Arrange
            var appUser = new AppUser
            {
                FullName = "AB", // Minimum 2 characters
                Description = "Test user description",
                UserName = "ab",
                Email = "ab@example.com"
            };

            // Act
            var validationResults = ValidateModel(appUser);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void AppUser_WithMaximumValidFullName_PassesValidation()
        {
            // Arrange
            var appUser = new AppUser
            {
                FullName = new string('A', 100), // Maximum 100 characters
                Description = "Test user description",
                UserName = "user",
                Email = "user@example.com"
            };

            // Act
            var validationResults = ValidateModel(appUser);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void AppUser_WithEmptyDescription_PassesValidation()
        {
            // Arrange
            var appUser = new AppUser
            {
                FullName = "John Doe",
                Description = "", // Description is optional
                UserName = "johndoe",
                Email = "john.doe@example.com"
            };

            // Act
            var validationResults = ValidateModel(appUser);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void AppUser_WithMaximumValidDescription_PassesValidation()
        {
            // Arrange
            var appUser = new AppUser
            {
                FullName = "John Doe",
                Description = new string('A', 500), // Maximum 500 characters
                UserName = "johndoe",
                Email = "john.doe@example.com"
            };

            // Act
            var validationResults = ValidateModel(appUser);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void AppUser_WithTooLongDescription_FailsValidation()
        {
            // Arrange
            var appUser = new AppUser
            {
                FullName = "John Doe",
                Description = new string('A', 501), // 501 characters, maximum is 500
                UserName = "johndoe",
                Email = "john.doe@example.com"
            };

            // Act
            var validationResults = ValidateModel(appUser);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("Description"));
        }

        [Fact]
        public void AppUser_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var appUser = new AppUser();

            // Assert
            Assert.Equal(string.Empty, appUser.FullName);
            Assert.Equal(string.Empty, appUser.Description);
        }

        [Fact]
        public void AppUser_InheritsFromIdentityUser_HasIdentityProperties()
        {
            // Arrange & Act
            var appUser = new AppUser
            {
                FullName = "John Doe",
                Description = "Test description",
                UserName = "johndoe",
                Email = "john@example.com",
                Id = "test-id"
            };

            // Assert - Verify it has IdentityUser properties
            Assert.NotNull(appUser.Id);
            Assert.NotNull(appUser.UserName);
            Assert.NotNull(appUser.Email);
            Assert.Equal("test-id", appUser.Id);
            Assert.Equal("johndoe", appUser.UserName);
            Assert.Equal("john@example.com", appUser.Email);
            
            // Verify custom properties
            Assert.Equal("John Doe", appUser.FullName);
            Assert.Equal("Test description", appUser.Description);
        }

        [Fact]
        public void AppUser_CanSetAndGetAllProperties()
        {
            // Arrange
            var appUser = new AppUser();

            // Act
            appUser.FullName = "Jane Smith";
            appUser.Description = "Updated description";
            appUser.UserName = "janesmith";
            appUser.Email = "jane@example.com";

            // Assert
            Assert.Equal("Jane Smith", appUser.FullName);
            Assert.Equal("Updated description", appUser.Description);
            Assert.Equal("janesmith", appUser.UserName);
            Assert.Equal("jane@example.com", appUser.Email);
        }

        [Theory]
        [InlineData("John")]
        [InlineData("John Doe")]
        [InlineData("Mary-Jane")]
        [InlineData("O'Connor")]
        [InlineData("Dr. Smith")]
        [InlineData("Jean-Pierre")]
        [InlineData("Van Der Berg")]
        public void AppUser_WithVariousValidFullNames_PassesValidation(string fullName)
        {
            // Arrange
            var appUser = new AppUser
            {
                FullName = fullName,
                Description = "Test description",
                UserName = "testuser",
                Email = "test@example.com"
            };

            // Act
            var validationResults = ValidateModel(appUser);

            // Assert
            Assert.Empty(validationResults);
        }

        [Theory]
        [InlineData("John123")]
        [InlineData("John@Doe")]
        [InlineData("John#Doe")]
        [InlineData("John$Doe")]
        [InlineData("John&Doe")]
        [InlineData("John*Doe")]
        [InlineData("John+Doe")]
        [InlineData("John=Doe")]
        public void AppUser_WithInvalidFullNameCharacters_FailsValidation(string fullName)
        {
            // Arrange
            var appUser = new AppUser
            {
                FullName = fullName,
                Description = "Test description",
                UserName = "testuser",
                Email = "test@example.com"
            };

            // Act
            var validationResults = ValidateModel(appUser);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("FullName"));
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
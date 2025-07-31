using System.ComponentModel.DataAnnotations;
using UserManagementAPI.Models;
using Xunit;

namespace UserManagementAPI_Testing.Models
{
    public class PaginationParametersTests
    {
        [Fact]
        public void PaginationParameters_WithDefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var parameters = new PaginationParameters();

            // Assert
            Assert.Equal(1, parameters.PageNumber);
            Assert.Equal(10, parameters.PageSize);
            Assert.Null(parameters.SearchTerm);
            Assert.Null(parameters.SortBy);
            Assert.False(parameters.SortDescending);
        }

        [Fact]
        public void PaginationParameters_WithValidValues_PassesValidation()
        {
            // Arrange
            var parameters = new PaginationParameters
            {
                PageNumber = 2,
                PageSize = 20,
                SearchTerm = "test",
                SortBy = "username",
                SortDescending = true
            };

            // Act
            var validationResults = ValidateModel(parameters);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void PaginationParameters_WithZeroPageNumber_FailsValidation()
        {
            // Arrange
            var parameters = new PaginationParameters
            {
                PageNumber = 0
            };

            // Act
            var validationResults = ValidateModel(parameters);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("PageNumber"));
        }

        [Fact]
        public void PaginationParameters_WithNegativePageNumber_FailsValidation()
        {
            // Arrange
            var parameters = new PaginationParameters
            {
                PageNumber = -1
            };

            // Act
            var validationResults = ValidateModel(parameters);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("PageNumber"));
        }

        [Fact]
        public void PaginationParameters_WithZeroPageSize_FailsValidation()
        {
            // Arrange
            var parameters = new PaginationParameters
            {
                PageSize = 0
            };

            // Act
            var validationResults = ValidateModel(parameters);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("PageSize"));
        }

        [Fact]
        public void PaginationParameters_WithNegativePageSize_FailsValidation()
        {
            // Arrange
            var parameters = new PaginationParameters
            {
                PageSize = -1
            };

            // Act
            var validationResults = ValidateModel(parameters);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("PageSize"));
        }

        [Fact]
        public void PaginationParameters_WithMaximumPageSize_IsLimitedTo100()
        {
            // Arrange
            var parameters = new PaginationParameters
            {
                PageSize = 150 // Should be limited to 100
            };

            // Act & Assert
            Assert.Equal(100, parameters.PageSize);
        }

        [Fact]
        public void PaginationParameters_WithPageSizeOver100_FailsValidation()
        {
            // Arrange
            var parameters = new PaginationParameters();
            
            // Use reflection to set the private field to test validation
            var field = typeof(PaginationParameters).GetField("_pageSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(parameters, 150);

            // Act
            var validationResults = ValidateModel(parameters);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("PageSize"));
        }

        [Fact]
        public void PaginationParameters_WithValidPageSize_PassesValidation()
        {
            // Arrange
            var parameters = new PaginationParameters
            {
                PageSize = 50
            };

            // Act
            var validationResults = ValidateModel(parameters);

            // Assert
            Assert.Empty(validationResults);
            Assert.Equal(50, parameters.PageSize);
        }

        [Fact]
        public void PaginationParameters_WithTooLongSortBy_FailsValidation()
        {
            // Arrange
            var parameters = new PaginationParameters
            {
                SortBy = new string('a', 51) // 51 characters, maximum is 50
            };

            // Act
            var validationResults = ValidateModel(parameters);

            // Assert
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("SortBy"));
        }

        [Fact]
        public void PaginationParameters_WithValidSortBy_PassesValidation()
        {
            // Arrange
            var parameters = new PaginationParameters
            {
                SortBy = "username"
            };

            // Act
            var validationResults = ValidateModel(parameters);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void PaginationParameters_WithEmptySearchTerm_PassesValidation()
        {
            // Arrange
            var parameters = new PaginationParameters
            {
                SearchTerm = ""
            };

            // Act
            var validationResults = ValidateModel(parameters);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void PaginationParameters_WithWhitespaceSearchTerm_PassesValidation()
        {
            // Arrange
            var parameters = new PaginationParameters
            {
                SearchTerm = "   "
            };

            // Act
            var validationResults = ValidateModel(parameters);

            // Assert
            Assert.Empty(validationResults);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(50)]
        [InlineData(100)]
        public void PaginationParameters_WithValidPageSizes_PassesValidation(int pageSize)
        {
            // Arrange
            var parameters = new PaginationParameters
            {
                PageSize = pageSize
            };

            // Act
            var validationResults = ValidateModel(parameters);

            // Assert
            Assert.Empty(validationResults);
            Assert.Equal(pageSize, parameters.PageSize);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(100)]
        [InlineData(1000)]
        public void PaginationParameters_WithValidPageNumbers_PassesValidation(int pageNumber)
        {
            // Arrange
            var parameters = new PaginationParameters
            {
                PageNumber = pageNumber
            };

            // Act
            var validationResults = ValidateModel(parameters);

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
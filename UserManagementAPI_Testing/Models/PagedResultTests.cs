using UserManagementAPI.Models;
using Xunit;

namespace UserManagementAPI_Testing.Models
{
    public class PagedResultTests
    {
        [Fact]
        public void PagedResult_WithDefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var pagedResult = new PagedResult<string>();

            // Assert
            Assert.NotNull(pagedResult.Items);
            Assert.Empty(pagedResult.Items);
            Assert.Equal(0, pagedResult.TotalCount);
            Assert.Equal(0, pagedResult.PageNumber);
            Assert.Equal(0, pagedResult.PageSize);
        }

        [Fact]
        public void PagedResult_TotalPages_CalculatedCorrectly()
        {
            // Arrange
            var pagedResult = new PagedResult<string>
            {
                TotalCount = 25,
                PageSize = 10
            };

            // Act & Assert
            Assert.Equal(3, pagedResult.TotalPages); // 25 / 10 = 2.5, rounded up = 3
        }

        [Fact]
        public void PagedResult_TotalPages_WithExactDivision_CalculatedCorrectly()
        {
            // Arrange
            var pagedResult = new PagedResult<string>
            {
                TotalCount = 30,
                PageSize = 10
            };

            // Act & Assert
            Assert.Equal(3, pagedResult.TotalPages); // 30 / 10 = 3
        }

        [Fact]
        public void PagedResult_TotalPages_WithZeroPageSize_HandlesGracefully()
        {
            // Arrange
            var pagedResult = new PagedResult<string>
            {
                TotalCount = 25,
                PageSize = 0
            };

            // Act & Assert
            // When PageSize is 0, TotalPages will be Infinity (25/0), not 0
            Assert.True(double.IsInfinity(pagedResult.TotalPages));
        }

        [Fact]
        public void PagedResult_TotalPages_WithZeroTotalCount_ReturnsZero()
        {
            // Arrange
            var pagedResult = new PagedResult<string>
            {
                TotalCount = 0,
                PageSize = 10
            };

            // Act & Assert
            Assert.Equal(0, pagedResult.TotalPages);
        }

        [Fact]
        public void PagedResult_HasPreviousPage_WithFirstPage_ReturnsFalse()
        {
            // Arrange
            var pagedResult = new PagedResult<string>
            {
                PageNumber = 1
            };

            // Act & Assert
            Assert.False(pagedResult.HasPreviousPage);
        }

        [Fact]
        public void PagedResult_HasPreviousPage_WithSecondPage_ReturnsTrue()
        {
            // Arrange
            var pagedResult = new PagedResult<string>
            {
                PageNumber = 2
            };

            // Act & Assert
            Assert.True(pagedResult.HasPreviousPage);
        }

        [Fact]
        public void PagedResult_HasNextPage_WithLastPage_ReturnsFalse()
        {
            // Arrange
            var pagedResult = new PagedResult<string>
            {
                TotalCount = 25,
                PageSize = 10,
                PageNumber = 3 // Last page (25 items, 10 per page = 3 pages)
            };

            // Act & Assert
            Assert.False(pagedResult.HasNextPage);
        }

        [Fact]
        public void PagedResult_HasNextPage_WithNotLastPage_ReturnsTrue()
        {
            // Arrange
            var pagedResult = new PagedResult<string>
            {
                TotalCount = 25,
                PageSize = 10,
                PageNumber = 2 // Not last page
            };

            // Act & Assert
            Assert.True(pagedResult.HasNextPage);
        }

        [Fact]
        public void PagedResult_HasNextPage_WithOnlyOnePage_ReturnsFalse()
        {
            // Arrange
            var pagedResult = new PagedResult<string>
            {
                TotalCount = 5,
                PageSize = 10,
                PageNumber = 1 // Only one page
            };

            // Act & Assert
            Assert.False(pagedResult.HasNextPage);
        }

        [Fact]
        public void PagedResult_WithItems_StoresItemsCorrectly()
        {
            // Arrange
            var items = new List<string> { "item1", "item2", "item3" };
            var pagedResult = new PagedResult<string>
            {
                Items = items,
                TotalCount = 3,
                PageSize = 10,
                PageNumber = 1
            };

            // Act & Assert
            Assert.Equal(items, pagedResult.Items);
            Assert.Equal(3, pagedResult.Items.Count());
        }

        [Fact]
        public void PagedResult_WithComplexType_WorksCorrectly()
        {
            // Arrange
            var users = new List<UserDto>
            {
                new UserDto { Id = "1", UserName = "user1", Email = "user1@test.com" },
                new UserDto { Id = "2", UserName = "user2", Email = "user2@test.com" }
            };

            var pagedResult = new PagedResult<UserDto>
            {
                Items = users,
                TotalCount = 50,
                PageSize = 10,
                PageNumber = 2
            };

            // Act & Assert
            Assert.Equal(users, pagedResult.Items);
            Assert.Equal(50, pagedResult.TotalCount);
            Assert.Equal(10, pagedResult.PageSize);
            Assert.Equal(2, pagedResult.PageNumber);
            Assert.Equal(5, pagedResult.TotalPages);
            Assert.True(pagedResult.HasPreviousPage);
            Assert.True(pagedResult.HasNextPage);
        }

        [Theory]
        [InlineData(10, 10, 1, false, false)]  // First and only page
        [InlineData(15, 10, 1, false, true)]   // First page with more
        [InlineData(25, 10, 2, true, true)]    // Middle page
        [InlineData(25, 10, 3, true, false)]   // Last page
        public void PagedResult_NavigationProperties_CalculatedCorrectly(
            int totalCount, int pageSize, int pageNumber,
            bool expectedHasPrevious, bool expectedHasNext)
        {
            // Arrange
            var pagedResult = new PagedResult<string>
            {
                TotalCount = totalCount,
                PageSize = pageSize,
                PageNumber = pageNumber
            };

            // Act & Assert
            Assert.Equal(expectedHasPrevious, pagedResult.HasPreviousPage);
            Assert.Equal(expectedHasNext, pagedResult.HasNextPage);
        }

        [Theory]
        [InlineData(0, 10, 0)]
        [InlineData(1, 10, 1)]
        [InlineData(10, 10, 1)]
        [InlineData(11, 10, 2)]
        [InlineData(25, 10, 3)]
        [InlineData(100, 10, 10)]
        public void PagedResult_TotalPages_VariousScenarios(int totalCount, int pageSize, int expectedTotalPages)
        {
            // Arrange
            var pagedResult = new PagedResult<string>
            {
                TotalCount = totalCount,
                PageSize = pageSize
            };

            // Act & Assert
            Assert.Equal(expectedTotalPages, pagedResult.TotalPages);
        }
    }
}
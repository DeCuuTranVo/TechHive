using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Models
{
    public class PaginationParameters
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 10;

        [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
        public int PageNumber { get; set; } = 1;

        [Range(1, MaxPageSize, ErrorMessage = "Page size must be between 1 and 100")]
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        public string? SearchTerm { get; set; }
        
        [StringLength(50, ErrorMessage = "Sort by field cannot exceed 50 characters")]
        public string? SortBy { get; set; }
        
        public bool SortDescending { get; set; } = false;
    }

    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public double TotalPages => PageSize > 0 ? Math.Ceiling((double)TotalCount / PageSize) : TotalCount > 0 ? double.PositiveInfinity : 0;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages && !double.IsInfinity(TotalPages);
    }
}
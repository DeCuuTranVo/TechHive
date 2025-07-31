using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;
using UserManagementAPI.Entities;

namespace UserManagementAPI.Services
{
    public interface IUserValidationService
    {
        Task<ValidationResult> ValidateCreateUserAsync(Models.CreateUserDto createUserDto);
        Task<ValidationResult> ValidateUpdateUserAsync(string userId, Models.UpdateUserDto updateUserDto);
        ValidationResult ValidateUserInput(string? input, string fieldName, int minLength, int maxLength, string? pattern = null);
    }

    public class UserValidationService : IUserValidationService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<UserValidationService> _logger;

        public UserValidationService(UserManager<AppUser> userManager, ILogger<UserValidationService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<ValidationResult> ValidateCreateUserAsync(Models.CreateUserDto createUserDto)
        {
            var result = new ValidationResult();

            try
            {
                // Check for duplicate email
                var existingUserByEmail = await _userManager.FindByEmailAsync(createUserDto.Email);
                if (existingUserByEmail != null)
                {
                    result.AddError("Email", "Email is already in use");
                }

                // Check for duplicate username
                var existingUserByName = await _userManager.FindByNameAsync(createUserDto.UserName);
                if (existingUserByName != null)
                {
                    result.AddError("UserName", "Username is already in use");
                }

                // Validate input formats
                var emailValidation = ValidateUserInput(createUserDto.Email, "Email", 1, 256, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!emailValidation.IsValid)
                {
                    result.Errors.AddRange(emailValidation.Errors);
                }

                var usernameValidation = ValidateUserInput(createUserDto.UserName, "UserName", 2, 50, @"^[a-zA-Z0-9_.-]+$");
                if (!usernameValidation.IsValid)
                {
                    result.Errors.AddRange(usernameValidation.Errors);
                }

                var fullNameValidation = ValidateUserInput(createUserDto.FullName, "FullName", 2, 100, @"^[a-zA-Z\s.'-]+$");
                if (!fullNameValidation.IsValid)
                {
                    result.Errors.AddRange(fullNameValidation.Errors);
                }

                // Validate password strength
                if (!ValidatePasswordStrength(createUserDto.Password))
                {
                    result.AddError("Password", "Password must contain at least one lowercase letter, one uppercase letter, one digit, and be at least 6 characters long");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user validation");
                result.AddError("General", "An error occurred during validation");
            }

            return result;
        }

        public async Task<ValidationResult> ValidateUpdateUserAsync(string userId, Models.UpdateUserDto updateUserDto)
        {
            var result = new ValidationResult();

            try
            {
                // Check for duplicate email (excluding current user)
                var existingUserByEmail = await _userManager.FindByEmailAsync(updateUserDto.Email);
                if (existingUserByEmail != null && existingUserByEmail.Id != userId)
                {
                    result.AddError("Email", "Email is already in use");
                }

                // Check for duplicate username (excluding current user)
                var existingUserByName = await _userManager.FindByNameAsync(updateUserDto.UserName);
                if (existingUserByName != null && existingUserByName.Id != userId)
                {
                    result.AddError("UserName", "Username is already in use");
                }

                // Validate input formats
                var emailValidation = ValidateUserInput(updateUserDto.Email, "Email", 1, 256, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!emailValidation.IsValid)
                {
                    result.Errors.AddRange(emailValidation.Errors);
                }

                var usernameValidation = ValidateUserInput(updateUserDto.UserName, "UserName", 2, 50, @"^[a-zA-Z0-9_.-]+$");
                if (!usernameValidation.IsValid)
                {
                    result.Errors.AddRange(usernameValidation.Errors);
                }

                var fullNameValidation = ValidateUserInput(updateUserDto.FullName, "FullName", 2, 100, @"^[a-zA-Z\s.'-]+$");
                if (!fullNameValidation.IsValid)
                {
                    result.Errors.AddRange(fullNameValidation.Errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user update validation");
                result.AddError("General", "An error occurred during validation");
            }

            return result;
        }

        public ValidationResult ValidateUserInput(string? input, string fieldName, int minLength, int maxLength, string? pattern = null)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(input))
            {
                result.AddError(fieldName, $"{fieldName} is required");
                return result;
            }

            var trimmedInput = input.Trim();

            if (trimmedInput.Length < minLength)
            {
                result.AddError(fieldName, $"{fieldName} must be at least {minLength} characters long");
            }

            if (trimmedInput.Length > maxLength)
            {
                result.AddError(fieldName, $"{fieldName} cannot exceed {maxLength} characters");
            }

            if (!string.IsNullOrEmpty(pattern) && !Regex.IsMatch(trimmedInput, pattern))
            {
                result.AddError(fieldName, $"{fieldName} contains invalid characters");
            }

            return result;
        }

        private static bool ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                return false;

            var hasLowercase = password.Any(char.IsLower);
            var hasUppercase = password.Any(char.IsUpper);
            var hasDigit = password.Any(char.IsDigit);

            return hasLowercase && hasUppercase && hasDigit;
        }
    }

    public class ValidationResult
    {
        public List<ValidationError> Errors { get; set; } = new();
        public bool IsValid => !Errors.Any();

        public void AddError(string field, string message)
        {
            Errors.Add(new ValidationError { Field = field, Message = message });
        }
    }

    public class ValidationError
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
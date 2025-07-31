using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using UserManagementAPI.Entities;
using UserManagementAPI.Models;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // Removed [Authorize] attribute - authentication is handled by TokenAuthenticationMiddleware
    public class UserController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<UserController> _logger;

        public UserController(UserManager<AppUser> userManager, ILogger<UserController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        // Simple test endpoint that doesn't require database access
        [HttpGet("test")]
        public IActionResult Test()
        {
            var currentUserId = HttpContext.Items["UserId"]?.ToString();
            var currentUserName = HttpContext.Items["UserName"]?.ToString();
            
            return Ok(new { 
                Message = "Authentication successful", 
                UserId = currentUserId,
                UserName = currentUserName,
                Authenticated = !string.IsNullOrEmpty(currentUserId)
            });
        }

        // GET: api/User
        [HttpGet]
        public async Task<ActionResult<PagedResult<UserDto>>> GetUsers([FromQuery] PaginationParameters parameters)
        {
            try
            {
                var currentUserId = HttpContext.Items["UserId"]?.ToString();
                _logger.LogInformation("User {UserId} retrieving users with pagination. Page: {PageNumber}, Size: {PageSize}, Search: {SearchTerm}", 
                    currentUserId, parameters.PageNumber, parameters.PageSize, parameters.SearchTerm);

                // Validate pagination parameters
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid pagination parameters provided by user {UserId}", currentUserId);
                    return BadRequest(ModelState);
                }

                var query = _userManager.Users.AsQueryable();

                // Apply search filter if provided
                if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
                {
                    var searchTerm = parameters.SearchTerm.Trim().ToLower();
                    query = query.Where(u => 
                        u.UserName!.ToLower().Contains(searchTerm) ||
                        u.Email!.ToLower().Contains(searchTerm) ||
                        u.FullName.ToLower().Contains(searchTerm));
                }

                // Apply sorting
                query = ApplySorting(query, parameters.SortBy, parameters.SortDescending);

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var users = await query
                    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToListAsync();

                var userDtos = users.Select(user => MapToUserDto(user));

                var result = new PagedResult<UserDto>
                {
                    Items = userDtos,
                    TotalCount = totalCount,
                    PageNumber = parameters.PageNumber,
                    PageSize = parameters.PageSize
                };

                _logger.LogInformation("Successfully retrieved {Count} users out of {TotalCount} for user {UserId}", 
                    users.Count, totalCount, currentUserId);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid arguments provided for user retrieval");
                return BadRequest("Invalid parameters provided");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while retrieving users");
                return StatusCode(500, "An error occurred while processing the request");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving users");
                return StatusCode(500, "An error occurred while retrieving users");
            }
        }

        // GET: api/User/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser([Required] string id)
        {
            try
            {
                var currentUserId = HttpContext.Items["UserId"]?.ToString();
                
                // Validate input
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("GetUser called with null or empty ID by user {UserId}", currentUserId);
                    return BadRequest("User ID cannot be null or empty");
                }

                _logger.LogInformation("User {UserId} retrieving user with ID: {TargetUserId}", currentUserId, id);

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} attempted to access non-existent user {TargetUserId}", currentUserId, id);
                    return NotFound("User not found");
                }

                var userDto = MapToUserDto(user);

                _logger.LogInformation("Successfully retrieved user {TargetUserId} for user {UserId}", id, currentUserId);
                return Ok(userDto);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid user ID format: {UserId}", id);
                return BadRequest("Invalid user ID format");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while retrieving user with ID {UserId}", id);
                return StatusCode(500, "An error occurred while processing the request");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving user with ID {UserId}", id);
                return StatusCode(500, "An error occurred while retrieving the user");
            }
        }

        // POST: api/User
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                var currentUserId = HttpContext.Items["UserId"]?.ToString();
                _logger.LogInformation("User {UserId} creating new user with username: {UserName}", currentUserId, createUserDto?.UserName);

                // Validate input
                if (createUserDto == null)
                {
                    _logger.LogWarning("CreateUser called with null DTO by user {UserId}", currentUserId);
                    return BadRequest("User data is required");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("CreateUser called with invalid model state by user {UserId}", currentUserId);
                    return BadRequest(ModelState);
                }

                // Additional business validation
                var existingUserByEmail = await _userManager.FindByEmailAsync(createUserDto.Email);
                if (existingUserByEmail != null)
                {
                    _logger.LogWarning("User {UserId} attempted to create user with existing email: {Email}", currentUserId, createUserDto.Email);
                    ModelState.AddModelError(nameof(createUserDto.Email), "Email is already in use");
                    return BadRequest(ModelState);
                }

                var existingUserByName = await _userManager.FindByNameAsync(createUserDto.UserName);
                if (existingUserByName != null)
                {
                    _logger.LogWarning("User {UserId} attempted to create user with existing username: {UserName}", currentUserId, createUserDto.UserName);
                    ModelState.AddModelError(nameof(createUserDto.UserName), "Username is already in use");
                    return BadRequest(ModelState);
                }

                var user = new AppUser
                {
                    UserName = createUserDto.UserName.Trim(),
                    Email = createUserDto.Email.Trim().ToLower(),
                    FullName = createUserDto.FullName.Trim(),
                    Description = createUserDto.Description?.Trim() ?? string.Empty
                };

                var result = await _userManager.CreateAsync(user, createUserDto.Password);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("User {UserId} failed to create user {UserName}. Errors: {Errors}", 
                        currentUserId, createUserDto.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return BadRequest(ModelState);
                }

                var userDto = MapToUserDto(user);

                _logger.LogInformation("User {UserId} successfully created user with ID: {NewUserId}", currentUserId, user.Id);
                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "Null argument provided during user creation");
                return BadRequest("Required data is missing");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation during user creation");
                return StatusCode(500, "An error occurred while processing the request");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while creating user");
                return StatusCode(500, "An error occurred while creating the user");
            }
        }

        // PUT: api/User/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser([Required] string id, [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                var currentUserId = HttpContext.Items["UserId"]?.ToString();
                _logger.LogInformation("User {UserId} updating user with ID: {TargetUserId}", currentUserId, id);

                // Validate input
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("UpdateUser called with null or empty ID by user {UserId}", currentUserId);
                    return BadRequest("User ID cannot be null or empty");
                }

                if (updateUserDto == null)
                {
                    _logger.LogWarning("UpdateUser called with null DTO by user {UserId}", currentUserId);
                    return BadRequest("User data is required");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("UpdateUser called with invalid model state for user {TargetUserId} by user {UserId}", id, currentUserId);
                    return BadRequest(ModelState);
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} attempted to update non-existent user {TargetUserId}", currentUserId, id);
                    return NotFound("User not found");
                }

                // Check for duplicate email (excluding current user)
                var existingUserByEmail = await _userManager.FindByEmailAsync(updateUserDto.Email);
                if (existingUserByEmail != null && existingUserByEmail.Id != id)
                {
                    _logger.LogWarning("User {UserId} attempted to update user {TargetUserId} with existing email: {Email}", currentUserId, id, updateUserDto.Email);
                    ModelState.AddModelError(nameof(updateUserDto.Email), "Email is already in use");
                    return BadRequest(ModelState);
                }

                // Check for duplicate username (excluding current user)
                var existingUserByName = await _userManager.FindByNameAsync(updateUserDto.UserName);
                if (existingUserByName != null && existingUserByName.Id != id)
                {
                    _logger.LogWarning("User {UserId} attempted to update user {TargetUserId} with existing username: {UserName}", currentUserId, id, updateUserDto.UserName);
                    ModelState.AddModelError(nameof(updateUserDto.UserName), "Username is already in use");
                    return BadRequest(ModelState);
                }

                // Update user properties
                user.UserName = updateUserDto.UserName.Trim();
                user.Email = updateUserDto.Email.Trim().ToLower();
                user.FullName = updateUserDto.FullName.Trim();
                user.Description = updateUserDto.Description?.Trim() ?? string.Empty;

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("User {UserId} failed to update user {TargetUserId}. Errors: {Errors}", 
                        currentUserId, id, string.Join(", ", result.Errors.Select(e => e.Description)));
                    
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("User {UserId} successfully updated user with ID: {TargetUserId}", currentUserId, id);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid arguments provided for user update: {UserId}", id);
                return BadRequest("Invalid parameters provided");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while updating user with ID {UserId}", id);
                return StatusCode(500, "An error occurred while processing the request");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while updating user with ID {UserId}", id);
                return StatusCode(500, "An error occurred while updating the user");
            }
        }

        // DELETE: api/User/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser([Required] string id)
        {
            try
            {
                var currentUserId = HttpContext.Items["UserId"]?.ToString();
                _logger.LogInformation("User {UserId} deleting user with ID: {TargetUserId}", currentUserId, id);

                // Validate input
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("DeleteUser called with null or empty ID by user {UserId}", currentUserId);
                    return BadRequest("User ID cannot be null or empty");
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} attempted to delete non-existent user {TargetUserId}", currentUserId, id);
                    return NotFound("User not found");
                }

                var result = await _userManager.DeleteAsync(user);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("User {UserId} failed to delete user {TargetUserId}. Errors: {Errors}", 
                        currentUserId, id, string.Join(", ", result.Errors.Select(e => e.Description)));
                    
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("User {UserId} successfully deleted user with ID: {TargetUserId}", currentUserId, id);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid user ID format for deletion: {UserId}", id);
                return BadRequest("Invalid user ID format");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while deleting user with ID {UserId}", id);
                return StatusCode(500, "An error occurred while processing the request");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while deleting user with ID {UserId}", id);
                return StatusCode(500, "An error occurred while deleting the user");
            }
        }

        private static UserDto MapToUserDto(AppUser user)
        {
            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Description = user.Description,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnd = user.LockoutEnd?.DateTime,
                LockoutEnabled = user.LockoutEnabled,
                AccessFailedCount = user.AccessFailedCount
            };
        }

        private static IQueryable<AppUser> ApplySorting(IQueryable<AppUser> query, string? sortBy, bool sortDescending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return query.OrderBy(u => u.UserName);
            }

            return sortBy.ToLower() switch
            {
                "username" => sortDescending ? query.OrderByDescending(u => u.UserName) : query.OrderBy(u => u.UserName),
                "email" => sortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                "fullname" => sortDescending ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName),
                "emailconfirmed" => sortDescending ? query.OrderByDescending(u => u.EmailConfirmed) : query.OrderBy(u => u.EmailConfirmed),
                _ => query.OrderBy(u => u.UserName)
            };
        }
    }
}

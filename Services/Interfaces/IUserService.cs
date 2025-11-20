using CrudCloud.api.DTOs;
using CrudCloud.api.Models;

namespace CrudCloud.api.Services;

public interface IUserService
{
    Task<User> RegisterAsync(UserRegisterDto dto);
    Task<User?> UpdateAsync(int id, UserUpdateDto dto);
    Task<UserDetailDto?> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetAllAsync();
    
    Task<User?> ToggleStatusAsync(int id);
    Task<string?> LoginAsync(UserLoginDto dto);
    
    Task<bool> VerifyEmailAsync(string token);
    Task<bool> RequestPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

}
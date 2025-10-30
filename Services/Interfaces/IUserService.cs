using CrudCloud.api.DTOs;
using CrudCloud.api.Models;

namespace CrudCloud.api.Services;

public interface IUserService
{
    Task<User> RegisterAsync(UserRegisterDto dto);
    Task<User?> UpdateAsync(int id, UserUpdateDto dto);
    Task<User?> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetAllAsync();
    
    Task<User?> ToggleStatusAsync(int id);
    Task<string?> LoginAsync(UserLoginDto dto);

}
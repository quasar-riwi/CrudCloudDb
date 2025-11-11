using CrudCloud.api.DTOs;


namespace CrudCloud.api.Services;

public interface IDatabaseInstanceService
{
    Task<IEnumerable<DatabaseInstanceDto>> GetUserInstancesAsync(int userId);
    Task<DatabaseInstanceDto> CreateInstanceAsync(int userId, DatabaseInstanceCreateDto dto);
    Task<bool> DeleteInstanceAsync(int userId, int id);
    
}
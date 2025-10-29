using CrudCloud.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrudCloud.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditLogsController(IAuditService auditService)
    {
        _auditService = auditService;
    }
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var logs = await _auditService.GetAllAsync();
        return Ok(logs);
    }
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var logs = await _auditService.GetByUserAsync(userId);
        return Ok(logs);
    }
}
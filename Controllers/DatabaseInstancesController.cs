using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using CrudCloud.api.DTOs;
using CrudCloud.api.Services;
using Microsoft.AspNetCore.Authorization;

namespace CrudCloud.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DatabaseInstancesController : ControllerBase
{
    private readonly IDatabaseInstanceService _service;
    private readonly IDiscordWebhookService _discordWebhookService;
    private readonly IUserService _userService;

    public DatabaseInstancesController(
        IDatabaseInstanceService service, 
        IDiscordWebhookService discordWebhookService,
        IUserService userService)
    {
        _service = service;
        _discordWebhookService = discordWebhookService;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { message = "Usuario no autenticado." });

        var list = await _service.GetUserInstancesAsync(userId);
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DatabaseInstanceCreateDto dto)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { message = "Usuario no autenticado." });

        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
            return Unauthorized(new { message = "Usuario no encontrado." });

        var result = await _service.CreateInstanceAsync(userId, dto);
        
        await _discordWebhookService.SendDatabaseCreatedAsync(
            result.Nombre, 
            dto.Motor.ToString(), 
            userId.ToString(), 
            $"{user.Nombre} {user.Apellido}"
        );
        
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { message = "Usuario no autenticado." });
        
        var userInstances = await _service.GetUserInstancesAsync(userId);
        var dbToDelete = userInstances.FirstOrDefault(db => db.Id == id);
        
        if (dbToDelete == null)
            return Forbid();
        
        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
            return Unauthorized(new { message = "Usuario no encontrado." });

        var ok = await _service.DeleteInstanceAsync(userId, id);
        if (!ok) return Forbid();
        
        await _discordWebhookService.SendDatabaseDeletedAsync(
            dbToDelete.Nombre, 
            dbToDelete.Motor, 
            userId.ToString(), 
            $"{user.Nombre} {user.Apellido}"
        );
        
        return NoContent();
    }
}
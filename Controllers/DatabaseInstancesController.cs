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

    /// <summary>
    /// Obtiene las instancias de base de datos del usuario autenticado.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // Obtiene el ID del usuario desde el token JWT
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { message = "Usuario no autenticado." });

        // Obtiene solo las instancias del usuario
        var instances = await _service.GetUserInstancesAsync(userId);
        return Ok(instances);
    }

    /// <summary>
    /// Crea una nueva instancia de base de datos para el usuario autenticado.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DatabaseInstanceCreateDto dto)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { message = "Usuario no autenticado." });

        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
            return Unauthorized(new { message = "Usuario no encontrado." });

        // Crea la instancia asociada al usuario autenticado
        var result = await _service.CreateInstanceAsync(userId, dto);

        // Notifica en Discord
        await _discordWebhookService.SendDatabaseCreatedAsync(
            result.Nombre,
            dto.Motor.ToString(),
            userId.ToString(),
            $"{user.Nombre} {user.Apellido}"
        );

        return Ok(result);
    }

    /// <summary>
    /// Elimina una instancia solo si pertenece al usuario autenticado.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { message = "Usuario no autenticado." });

        // Obtiene las instancias del usuario autenticado
        var userInstances = await _service.GetUserInstancesAsync(userId);
        var dbToDelete = userInstances.FirstOrDefault(db => db.Id == id);

        // Verifica propiedad
        if (dbToDelete == null)
            return Forbid("No tienes permiso para eliminar esta instancia.");

        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
            return Unauthorized(new { message = "Usuario no encontrado." });

        var success = await _service.DeleteInstanceAsync(userId, id);
        if (!success) return Forbid();

        // Notifica eliminación
        await _discordWebhookService.SendDatabaseDeletedAsync(
            dbToDelete.Nombre,
            dbToDelete.Motor,
            userId.ToString(),
            $"{user.Nombre} {user.Apellido}"
        );

        return NoContent();
    }
}

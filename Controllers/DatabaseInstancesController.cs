using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using CrudCloud.api.DTOs;
using CrudCloud.api.Services;
using Microsoft.AspNetCore.Authorization;

namespace CrudCloud.api.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class DatabaseInstancesController : ControllerBase
{
    private readonly IDatabaseInstanceService _service;

    public DatabaseInstancesController(IDatabaseInstanceService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // Obtener ID real del usuario autenticado
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { message = "Usuario no autenticado." });

        var list = await _service.GetUserInstancesAsync(userId);
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DatabaseInstanceCreateDto dto)
    {
        var result = await _service.CreateInstanceAsync(dto.UsuarioId, dto);
        return Ok(result);
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { message = "Usuario no autenticado." });

        var ok = await _service.DeleteInstanceAsync(userId, id);
        if (!ok) return Forbid();
        return NoContent();
    }
}


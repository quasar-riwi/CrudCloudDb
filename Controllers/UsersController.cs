using CrudCloud.api.DTOs;
using CrudCloud.api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrudCloud.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// Autentica un usuario y devuelve un token JWT

    [HttpPost("login")]
    [AllowAnonymous] 
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var token = await _userService.LoginAsync(dto);

            if (token == null)
                return Unauthorized(new { message = "Correo o contraseña incorrectos." });

            return Ok(new
            {
                message = "Login exitoso.",
                token = token,
                tokenType = "Bearer",
                expiresIn = "60 minutos"
            });
        }
        catch (InvalidOperationException ex)
        {
            // Usuario desactivado
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
    

    /// Registra un nuevo usuario en la plataforma

    [HttpPost("register")]
    [AllowAnonymous] // ⭐ Público - No requiere JWT
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var user = await _userService.RegisterAsync(dto);
            
            return CreatedAtAction(
                nameof(GetUserById),
                new { id = user.Id },
                new
                {
                    message = "Usuario registrado correctamente.",
                    userId = user.Id,
                    correo = user.Correo,
                    plan = user.Plan
                }
            );
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
    
    /// Obtiene la lista de todos los usuarios registrados
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllAsync();
        
        return Ok(new
        {
            message = "Usuarios obtenidos correctamente.",
            count = users.Count(),
            data = users
        });
    }
    
    /// Obtiene el detalle completo de un usuario específico
   
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userService.GetByIdAsync(id);

        if (user == null)
            return NotFound(new { message = $"Usuario con ID {id} no encontrado." });

        return Ok(new
        {
            message = "Usuario obtenido correctamente.",
            data = user
        });
    }
    
    /// Actualiza los datos de un usuario existente
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(int id, [FromBody] UserUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userService.UpdateAsync(id, dto);
        
        if (user == null)
            return NotFound(new { message = "Usuario no encontrado." });

        return Ok(new
        {
            message = "Usuario actualizado correctamente.",
            data = user
        });
    }
    
    // Cambiar Estado del Usuario (Activo/Inactivo)
    [HttpPatch("{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ToggleUserStatus(int id)
    {
        var user = await _userService.ToggleStatusAsync(id);

        if (user == null)
            return NotFound(new { message = $"Usuario con ID {id} no encontrado." });

        string statusMessage = user.IsActive ? "activado" : "desactivado";

        return Ok(new
        {
            message = $"Usuario {statusMessage} exitosamente.",
            data = new
            {
                userId = user.Id,
                nombre = user.Nombre,
                apellido = user.Apellido,
                correo = user.Correo,
                isActive = user.IsActive,
                plan = user.Plan
            }
        });
    }
}
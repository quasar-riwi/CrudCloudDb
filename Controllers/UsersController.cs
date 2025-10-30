﻿using CrudCloud.api.DTOs;
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
    
    // ============================================
// ✅ NUEVO: Verificar Email
// ============================================
/// <summary>
/// Verifica el correo electrónico del usuario mediante token
/// </summary>
/// <param name="token">Token de verificación enviado por email</param>
[HttpGet("verify-email")]
[AllowAnonymous]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> VerifyEmail([FromQuery] string token)
{
    if (string.IsNullOrEmpty(token))
        return BadRequest(new { message = "Token de verificación no proporcionado." });

    try
    {
        var result = await _userService.VerifyEmailAsync(token);

        if (!result)
            return BadRequest(new { message = "Token de verificación inválido." });

        return Ok(new
        {
            message = "¡Correo verificado exitosamente! Tu cuenta ha sido activada.",
            success = true
        });
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}

// ============================================
// 🔑 NUEVO: Solicitar Recuperación de Contraseña
// ============================================
/// <summary>
/// Solicita un enlace de recuperación de contraseña por email
/// </summary>
[HttpPost("forgot-password")]
[AllowAnonymous]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    var result = await _userService.RequestPasswordResetAsync(dto.Email);

    // Por seguridad, siempre retornar éxito (no revelar si el email existe)
    return Ok(new
    {
        message = "Si el correo existe en nuestro sistema, recibirás un enlace de recuperación.",
        success = true
    });
}

// ============================================
// 🔄 NUEVO: Resetear Contraseña con Token
// ============================================
/// <summary>
/// Resetea la contraseña usando el token de recuperación
/// </summary>
[HttpPost("reset-password")]
[AllowAnonymous]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    try
    {
        var result = await _userService.ResetPasswordAsync(dto.Token, dto.NewPassword);

        if (!result)
            return BadRequest(new { message = "Token de recuperación inválido." });

        return Ok(new
        {
            message = "Contraseña restablecida exitosamente. Ya puedes iniciar sesión.",
            success = true
        });
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}

// ============================================
// 🔐 NUEVO: Cambiar Contraseña (Usuario Autenticado)
// ============================================
/// <summary>
/// Permite al usuario cambiar su contraseña actual
/// </summary>
[HttpPost("change-password")]
[Authorize] // ⭐ Requiere JWT
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    // Obtener el ID del usuario desde el token JWT
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
    if (userIdClaim == null)
        return Unauthorized(new { message = "No se pudo identificar al usuario." });

    var userId = int.Parse(userIdClaim.Value);

    try
    {
        var result = await _userService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);

        if (!result)
            return BadRequest(new { message = "No se pudo cambiar la contraseña." });

        return Ok(new
        {
            message = "Contraseña cambiada exitosamente.",
            success = true
        });
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}
}
using CrudCloud.api.Data;
using CrudCloud.api.DTOs;
using CrudCloud.api.Models;
using CrudCloud.api.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CrudCloud.api.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService; // ⭐ NUEVO

    public UserService(AppDbContext context, IConfiguration configuration, IEmailService emailService)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
    }

    public async Task<User> RegisterAsync(UserRegisterDto dto)
    {
        // Validar correo único
        if (await _context.Users.AnyAsync(u => u.Correo == dto.Correo))
            throw new InvalidOperationException("El correo ya está registrado.");

        // Generar token de verificación
        var verificationToken = TokenGenerator.GenerateToken();
        var tokenExpiration = TokenGenerator.GenerateExpirationDate(24); // 24 horas

        var user = new User
        {
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            Correo = dto.Correo,
            Contraseña = PasswordHasher.HashPassword(dto.Contraseña),
            Plan = dto.Plan,
            IsActive = false, // Bloqueado hasta verificar email
            EmailVerified = false,
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpires = tokenExpiration
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Enviar email de verificación
        try
        {
            await _emailService.SendEmailVerificationAsync(user.Correo, user.Nombre, verificationToken);
        }
        catch (Exception ex)
        {
            // Log del error pero no fallar el registro
            Console.WriteLine($"Error enviando email de verificación: {ex.Message}");
        }

        return user;
    }

    public async Task<string?> LoginAsync(UserLoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Correo == dto.Correo);
        
        if (user == null) return null;
        
        if (!PasswordHasher.VerifyPassword(dto.Contraseña, user.Contraseña))
            return null;
        
        // Diferenciar entre no verificado y desactivado
        if (!user.EmailVerified)
            throw new InvalidOperationException("Debes verificar tu correo electrónico antes de iniciar sesión. Revisa tu bandeja de entrada.");
        
        if (!user.IsActive)
            throw new InvalidOperationException("Tu cuenta ha sido desactivada por un administrador. Contacta con soporte.");
        
        return GenerateJwtToken(user);
    }

    //  Verificar email
    public async Task<bool> VerifyEmailAsync(string token)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
        
        if (user == null)
            return false;
        
        // Verificar que el token no haya expirado
        if (user.EmailVerificationTokenExpires < DateTime.UtcNow)
            throw new InvalidOperationException("El token de verificación ha expirado. Solicita uno nuevo.");
        
        // Activar cuenta
        user.EmailVerified = true;
        user.IsActive = true;
        user.EmailVerificationToken = null; // Limpiar token usado
        user.EmailVerificationTokenExpires = null;
        
        await _context.SaveChangesAsync();
        
        // Enviar email de bienvenida
        try
        {
            await _emailService.SendWelcomeEmailAsync(user.Correo, user.Nombre);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando email de bienvenida: {ex.Message}");
        }
        
        return true;
    }

    //  Solicitar recuperación de contraseña
     public async Task<bool> RequestPasswordResetAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Correo == email);
        
        if (user == null)
            return true; // Por seguridad, no revelar si el email existe o no.
        
        var resetToken = TokenGenerator.GenerateToken();
        var tokenExpiration = TokenGenerator.GenerateExpirationDate(1); // 1 hora de expiración es más seguro.
        
        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpires = tokenExpiration;
        
        await _context.SaveChangesAsync();
        
        try
        {
            // --- Llamada correcta: Enviar el correo con el enlace de reseteo. ---
            await _emailService.SendPasswordResetAsync(user.Correo, user.Nombre, resetToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando email de recuperación: {ex.Message}");
        }
        
        return true;
    }

    //  Este método debe llamar a SendPasswordResetSuccessAsync para CONFIRMAR EL RESTABLECIMIENTO.
    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);
        
        if (user == null)
            return false;
        
        if (user.PasswordResetTokenExpires < DateTime.UtcNow)
            throw new InvalidOperationException("El token de recuperación ha expirado. Solicita uno nuevo.");
        
        user.Contraseña = PasswordHasher.HashPassword(newPassword);
        user.PasswordResetToken = null; 
        user.PasswordResetTokenExpires = null;
        
        await _context.SaveChangesAsync();
        
        try
        {
            // --- Llamada correcta: Enviar el correo de confirmación de que el reseteo fue exitoso. ---
            await _emailService.SendPasswordResetSuccessAsync(user.Correo, user.Nombre);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando email de confirmación de reseteo: {ex.Message}");
        }
        
        return true;
    }

    //  Este método llama a SendPasswordChangedConfirmationAsync, lo cual es correcto para esta acción.
    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
            return false;
        
        if (!PasswordHasher.VerifyPassword(currentPassword, user.Contraseña))
            throw new InvalidOperationException("La contraseña actual es incorrecta.");
        
        user.Contraseña = PasswordHasher.HashPassword(newPassword);
        await _context.SaveChangesAsync();
        
        try
        {
            // --- Esta llamada ya era correcta. ---
            await _emailService.SendPasswordChangedConfirmationAsync(user.Correo, user.Nombre);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando email de confirmación de cambio de contraseña: {ex.Message}");
        }
        
        return true;
    }


    public async Task<User?> UpdateAsync(int id, UserUpdateDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        // Si cambia el plan, enviar notificación
        string? oldPlan = null;
        if (user.Plan != dto.Plan)
        {
            oldPlan = user.Plan;
        }

        user.Nombre = dto.Nombre;
        user.Apellido = dto.Apellido;
        user.Correo = dto.Correo;
        user.Plan = dto.Plan;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        // Enviar email de cambio de plan
        if (oldPlan != null && oldPlan != dto.Plan)
        {
            try
            {
                await _emailService.SendPlanUpgradeNotificationAsync(user.Correo, user.Nombre, oldPlan, dto.Plan);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando email de cambio de plan: {ex.Message}");
            }
        }

        return user;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.Instancias)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<User?> ToggleStatusAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        user.IsActive = !user.IsActive;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        
        return user;
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"]!;
        var jwtIssuer = _configuration["Jwt:Issuer"]!;
        var jwtAudience = _configuration["Jwt:Audience"]!;
        var jwtExpirationMinutes = int.Parse(_configuration["Jwt:ExpirationInMinutes"]!);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Correo),
            new Claim(ClaimTypes.Name, $"{user.Nombre} {user.Apellido}"),
            new Claim("Plan", user.Plan),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
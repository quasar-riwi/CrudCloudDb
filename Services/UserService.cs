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

    // - Inyectar IConfiguration
    public UserService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<User> RegisterAsync(UserRegisterDto dto)
    {
        // Validar correo único
        if (await _context.Users.AnyAsync(u => u.Correo == dto.Correo))
            throw new InvalidOperationException("El correo ya está registrado.");

        var user = new User
        {
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            Correo = dto.Correo,
            Contraseña = PasswordHasher.HashPassword(dto.Contraseña),
            Plan = dto.Plan,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User?> UpdateAsync(int id, UserUpdateDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        user.Nombre = dto.Nombre;
        user.Apellido = dto.Apellido;
        user.Correo = dto.Correo;
        user.Plan = dto.Plan;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
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
    
    public async Task<string?> LoginAsync(UserLoginDto dto)
    {
        // Buscar usuario por correo
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Correo == dto.Correo);
        
        //  Validar que el usuario existe
        if (user == null)
            return null; // Usuario no encontrado

        //  Verificar que la contraseña es correcta
        if (!PasswordHasher.VerifyPassword(dto.Contraseña, user.Contraseña))
            return null; // Contraseña incorrecta

        //  Validar que el usuario está activo
        if (!user.IsActive)
            throw new InvalidOperationException("Tu cuenta ha sido desactivada. Contacta al administrador.");

        //  Generar el token JWT
        return GenerateJwtToken(user);
    }

    // MÉTODO PRIVADO - Generar JWT
    private string GenerateJwtToken(User user)
    {
        // Leer configuración de appsettings.json
        var jwtKey = _configuration["Jwt:Key"]!;
        var jwtIssuer = _configuration["Jwt:Issuer"]!;
        var jwtAudience = _configuration["Jwt:Audience"]!;
        var jwtExpirationMinutes = int.Parse(_configuration["Jwt:ExpirationInMinutes"]!);

        // Crear los claims (información del usuario en el token)
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Correo),
            new Claim(ClaimTypes.Name, $"{user.Nombre} {user.Apellido}"),
            new Claim("Plan", user.Plan),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Crear la clave de seguridad
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Crear el token
        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtExpirationMinutes),
            signingCredentials: credentials
        );

        // Convertir el token a string
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
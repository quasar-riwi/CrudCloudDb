using CrudCloud.api.Data;
using CrudCloud.api.Middlewares;
using CrudCloud.api.Repositories;
using CrudCloud.api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CrudCloud.api.Models;

var builder = WebApplication.CreateBuilder(args);


// 1. DEFINIR POLÍTICA DE CORS
var frontendAppPolicy = "FrontendAppPolicy";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: frontendAppPolicy, policy =>
    {
        policy
            .WithOrigins("https://quasar.andrescortes.dev",
                "http://localhost:5173",
                "http://localhost:8080",
                "http://localhost:8081",
                "http://localhost:5174") 
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); 
    });
});


// 2. HTTP CLIENT (PARA DISCORD WEBHOOKS)
builder.Services.AddHttpClient();

// 3. DATABASE
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresAdmin")));

// 4. CONFIGURATION SETTINGS
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<DiscordWebhookSettings>(builder.Configuration.GetSection("DiscordWebhookSettings"));

// 5. SERVICES
builder.Services.AddScoped<IDatabaseInstanceRepository, DatabaseInstanceRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IDatabaseInstanceService, DatabaseInstanceService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IDiscordWebhookService, DiscordWebhookService>(); // ✅ AGREGADO

// 6. AUTOMAPPER
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// 7. JWT AUTHENTICATION
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
        };
        
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (!string.IsNullOrEmpty(token))
                    context.Token = token;

                return Task.CompletedTask;
            }
        };
    });

// 8. CONTROLLERS & SWAGGER
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingrese 'Bearer' seguido de un espacio y el token JWT.\n\nEjemplo: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 9. MIDDLEWARE PIPELINE
app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI();
app.UseRouting();
app.UseCors("FrontendAppPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditMiddleware>();
app.MapControllers();

app.Run();
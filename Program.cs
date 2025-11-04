using CrudCloud.api.Data;
using CrudCloud.api.Middlewares;
using CrudCloud.api.Repositories;
using CrudCloud.api.Services;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresAdmin")));

builder.Services.AddScoped<IDatabaseInstanceRepository, DatabaseInstanceRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IDatabaseInstanceService, DatabaseInstanceService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddScoped<IUserService, UserService>();

// AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// F. Habilitar el uso de Controladores para nuestra API
builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();
app.UseMiddleware<AuditMiddleware>();
app.MapControllers();

app.Run();
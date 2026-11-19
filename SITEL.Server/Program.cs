using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SITEL.Server.Data;

var builder = WebApplication.CreateBuilder(args);

// Registrar DbContext para SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurar CORS para permitir comunicación desde el cliente Blazor WebAssembly
builder.Services.AddCors(options =>
{
    options.AddPolicy("SitelCorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configurar autenticación JWT (con fallback seguro si no está en UserSecrets)
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ClaveSuperSecretaParaElProyectoSITEL2026Bolivia#12345";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SITEL.Server";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SITEL.Client";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

var app = builder.Build();

// Inicializar esquema complementario y datos semilla de catálogos logísticos
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.InitializeAsync(dbContext);
}

app.UseHttpsRedirection();

app.UseCors("SitelCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

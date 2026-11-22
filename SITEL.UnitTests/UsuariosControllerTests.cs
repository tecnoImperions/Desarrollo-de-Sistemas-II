using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using SITEL.Server.Controllers;
using SITEL.Server.Data;
using SITEL.Shared.Entities;
using SITEL.Shared.Enums;
using Xunit;

namespace SITEL.UnitTests
{
    public class UsuariosControllerTests
    {
        private static AppDbContext CrearContexto()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"SitelUnit_Usuarios_{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new AppDbContext(options);
        }

        private static IConfiguration CrearConfigJwt()
        {
            var settings = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ClaveSuperSecretaDePruebasUnitariasSITEL2026#12345",
                ["Jwt:Issuer"] = "SITEL.Server",
                ["Jwt:Audience"] = "SITEL.Client"
            };
            return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        }

        [Fact]
        public async Task RegistrarUsuario_DebeHashearPasswordConBCryptYRechazarCorreoDuplicado()
        {
            await using var db = CrearContexto();
            var controller = new UsuariosController(db, CrearConfigJwt());

            var nuevo = new Usuario
            {
                NombreCompleto = "Prueba Operador",
                Email = "prueba@sitel.bo",
                PasswordHash = "Secreto123*",
                Rol = RolUsuario.OperadorLogistico
            };

            var creado = await controller.RegistrarUsuario(nuevo);
            Assert.IsType<CreatedAtActionResult>(creado.Result);

            var guardado = await db.Usuarios.FirstAsync(u => u.Email == "prueba@sitel.bo");
            Assert.NotEqual("Secreto123*", guardado.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("Secreto123*", guardado.PasswordHash));

            var duplicado = await controller.RegistrarUsuario(new Usuario
            {
                NombreCompleto = "Otro",
                Email = "prueba@sitel.bo",
                PasswordHash = "Otra123*",
                Rol = RolUsuario.Conductor
            });
            Assert.IsType<BadRequestObjectResult>(duplicado.Result);
        }

        [Fact]
        public async Task Login_CredencialesValidas_RetornaOkConTokenYCredencialesInvalidas_Retorna401()
        {
            await using var db = CrearContexto();
            db.Usuarios.Add(new Usuario
            {
                NombreCompleto = "Admin Test",
                Email = "admin@sitel.bo",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123*"),
                Rol = RolUsuario.Administrador
            });
            await db.SaveChangesAsync();

            var controller = new UsuariosController(db, CrearConfigJwt());

            var ok = await controller.Login(new LoginDto { Email = "admin@sitel.bo", Password = "Admin123*" });
            Assert.IsType<OkObjectResult>(ok);

            var unauth = await controller.Login(new LoginDto { Email = "admin@sitel.bo", Password = "Incorrecta" });
            Assert.IsType<UnauthorizedObjectResult>(unauth);
        }
    }
}

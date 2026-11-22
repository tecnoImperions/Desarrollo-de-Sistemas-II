using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SITEL.Server.Data;
using SITEL.Shared.DTOs;
using SITEL.Shared.Entities;
using Xunit;

namespace SITEL.IntegrationTests
{
    public class ClientesIntegrationTests : IClassFixture<SitelWebApplicationFactory>
    {
        private readonly SitelWebApplicationFactory _factory;

        public ClientesIntegrationTests(SitelWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CrearCliente_NitDuplicado_DebeRetornarBadRequest()
        {
            var client = _factory.CreateClient();
            var nit = $"NIT-{Guid.NewGuid().ToString()[..6]}";

            var primero = await client.PostAsJsonAsync("api/clientes", new Cliente
            {
                NombreRazonSocial = "Empresa Uno S.R.L.",
                DocumentoNit = nit,
                Correo = "uno@empresa.bo",
                Telefono = "70000001",
                Direccion = "Av. Principal 100"
            });
            Assert.Equal(HttpStatusCode.Created, primero.StatusCode);

            var segundo = await client.PostAsJsonAsync("api/clientes", new Cliente
            {
                NombreRazonSocial = "Empresa Dos S.R.L.",
                DocumentoNit = nit,
                Correo = "dos@empresa.bo",
                Telefono = "70000002",
                Direccion = "Av. Secundaria 200"
            });
            Assert.Equal(HttpStatusCode.BadRequest, segundo.StatusCode);
        }

        [Fact]
        public async Task EliminarCliente_ConEnviosAsociados_DebeBloquearPorIntegridad()
        {
            var client = _factory.CreateClient();
            int clienteId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                clienteId = db.Clientes.First().Id;
            }

            await client.PostAsJsonAsync("api/envios", new CrearEnvioRequestDto
            {
                Guia = $"SIT-CLI-{Guid.NewGuid().ToString()[..6]}",
                ClienteId = clienteId,
                Destinatario = "Contacto",
                DireccionEntrega = "Calle 1",
                DescripcionCarga = "Mercadería"
            });

            var deleteResp = await client.DeleteAsync($"api/clientes/{clienteId}");
            Assert.Equal(HttpStatusCode.BadRequest, deleteResp.StatusCode);
        }
    }
}

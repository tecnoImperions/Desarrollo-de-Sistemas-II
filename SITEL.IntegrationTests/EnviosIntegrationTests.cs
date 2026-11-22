using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SITEL.Server.Data;
using SITEL.Shared.DTOs;
using SITEL.Shared.Entities;
using SITEL.Shared.Enums;
using Xunit;

namespace SITEL.IntegrationTests
{
    public class EnviosIntegrationTests : IClassFixture<SitelWebApplicationFactory>
    {
        private readonly SitelWebApplicationFactory _factory;

        public EnviosIntegrationTests(SitelWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task FlujoCompleto_CrearAsignarActualizarYRastrearEnvio_DebeCumplirRN01RF02RF03RF04()
        {
            var client = _factory.CreateClient();

            int clienteId;
            int productoId;
            int stockInicial;
            int conductorId;
            int rutaId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                clienteId = db.Clientes.First().Id;
                var prod = db.Productos.First();
                productoId = prod.Id;
                stockInicial = prod.StockActual;
                conductorId = db.Conductores.First(c => !c.Nombres.Contains("Sin Asignar")).Id;
                rutaId = db.Rutas.First(r => !r.Nombre.Contains("Pendiente")).Id;
            }

            var guia = $"SIT-INT-{Guid.NewGuid().ToString()[..6].ToUpper()}";

            // 1. Crear envío sin conductor asignado (RN01 + descuento de stock)
            var crearResp = await client.PostAsJsonAsync("api/envios", new CrearEnvioRequestDto
            {
                Guia = guia,
                ClienteId = clienteId,
                Destinatario = "Recepción Central",
                DireccionEntrega = "Av. Banzer Km 5",
                DescripcionCarga = "Cajas de prueba integración",
                ItemsCarga = new List<ItemCargaRequestDto>
                {
                    new() { ProductoId = productoId, Cantidad = 2 }
                }
            });
            Assert.Equal(HttpStatusCode.Created, crearResp.StatusCode);

            var envioCreado = await crearResp.Content.ReadFromJsonAsync<Envio>();
            Assert.NotNull(envioCreado);
            Assert.Equal(EstadoEnvio.Registrado, envioCreado!.EstadoActual);

            // Verificar RN01: guía duplicada rechazada
            var dupResp = await client.PostAsJsonAsync("api/envios", new CrearEnvioRequestDto
            {
                Guia = guia,
                ClienteId = clienteId,
                Destinatario = "Otro",
                DireccionEntrega = "Otra",
                DescripcionCarga = "Duplicado"
            });
            Assert.Equal(HttpStatusCode.BadRequest, dupResp.StatusCode);

            // 2. Verificar RF02: no permite pasar a EnRuta sin conductor asignado
            var sinConductorResp = await client.PutAsJsonAsync($"api/envios/{envioCreado.Id}/estado", new ActualizarEstadoEnvioDto
            {
                NuevoEstado = EstadoEnvio.EnRuta,
                Ubicacion = "Almacén Central"
            });
            Assert.Equal(HttpStatusCode.BadRequest, sinConductorResp.StatusCode);

            // 3. Asignar conductor y ruta (RF02)
            var asignarResp = await client.PutAsJsonAsync($"api/envios/{envioCreado.Id}/asignar", new AsignarDespachoDto
            {
                ConductorId = conductorId,
                RutaId = rutaId
            });
            Assert.Equal(HttpStatusCode.OK, asignarResp.StatusCode);

            // 4. Actualizar estado a EnRuta y luego Entregado (RF03)
            var enRutaResp = await client.PutAsJsonAsync($"api/envios/{envioCreado.Id}/estado", new ActualizarEstadoEnvioDto
            {
                NuevoEstado = EstadoEnvio.EnRuta,
                Ubicacion = "Carretera al Norte"
            });
            Assert.Equal(HttpStatusCode.OK, enRutaResp.StatusCode);

            var entregadoResp = await client.PutAsJsonAsync($"api/envios/{envioCreado.Id}/estado", new ActualizarEstadoEnvioDto
            {
                NuevoEstado = EstadoEnvio.Entregado,
                Ubicacion = "Destino Final",
                ReceptorNombre = "María López"
            });
            Assert.Equal(HttpStatusCode.OK, entregadoResp.StatusCode);

            // 5. Consulta pública de rastreo RF04 ([AllowAnonymous])
            var rastreoResp = await client.GetAsync($"api/envios/rastreo/{guia}");
            Assert.Equal(HttpStatusCode.OK, rastreoResp.StatusCode);

            var rastreo = await rastreoResp.Content.ReadFromJsonAsync<RastreoEnvioDto>();
            Assert.NotNull(rastreo);
            Assert.Equal(guia, rastreo!.Guia);
            Assert.Equal("Entregado", rastreo.EstadoActual);
            Assert.True(rastreo.Historial.Count >= 3);

            // Verificar descuento de stock
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var prod = await db.Productos.FindAsync(productoId);
                Assert.Equal(stockInicial - 2, prod!.StockActual);
            }
        }

        [Fact]
        public async Task AnularEnvio_DebeRestituirStockYRechazarCambiosPosteriores_RN03()
        {
            var client = _factory.CreateClient();

            int clienteId;
            int productoId;
            int stockAntes;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                clienteId = db.Clientes.First().Id;
                var prod = db.Productos.Last();
                productoId = prod.Id;
                stockAntes = prod.StockActual;
            }

            var guia = $"SIT-ANU-{Guid.NewGuid().ToString()[..6].ToUpper()}";
            var crearResp = await client.PostAsJsonAsync("api/envios", new CrearEnvioRequestDto
            {
                Guia = guia,
                ClienteId = clienteId,
                Destinatario = "Cliente Anulación",
                DireccionEntrega = "Zona Sur",
                DescripcionCarga = "Carga a anular",
                ItemsCarga = new List<ItemCargaRequestDto>
                {
                    new() { ProductoId = productoId, Cantidad = 3 }
                }
            });
            Assert.Equal(HttpStatusCode.Created, crearResp.StatusCode);
            var envio = await crearResp.Content.ReadFromJsonAsync<Envio>();

            var anularResp = await client.PutAsJsonAsync($"api/envios/{envio!.Id}/anular", new AnularEnvioDto
            {
                MotivoAnulacion = "Error de digitación en orden",
                AutorizadoPor = "Administrador SITEL"
            });
            Assert.Equal(HttpStatusCode.OK, anularResp.StatusCode);

            // Stock restituido
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var prod = await db.Productos.FindAsync(productoId);
                Assert.Equal(stockAntes, prod!.StockActual);
            }

            // No permite cambiar estado tras anulación
            var estadoResp = await client.PutAsJsonAsync($"api/envios/{envio.Id}/estado", new ActualizarEstadoEnvioDto
            {
                NuevoEstado = EstadoEnvio.EnRuta,
                Ubicacion = "Almacén"
            });
            Assert.Equal(HttpStatusCode.BadRequest, estadoResp.StatusCode);
        }

        [Fact]
        public async Task RastrearPorGuia_Inexistente_DebeRetornar404()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("api/envios/rastreo/GUIA-NO-EXISTE-999");
            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }
    }
}

using System.Net;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using SITEL.Client.Layout;
using SITEL.Client.Pages;
using SITEL.Client.Services;
using SITEL.Shared.DTOs;
using SITEL.Shared.Entities;
using SITEL.Shared.Enums;
using Xunit;

namespace SITEL.UITests
{
    public class AccesoYMenuUITests : TestContext
    {
        private void RegistrarServiciosBase(MockHttpMessageHandler? handler = null)
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            var httpHandler = handler ?? new MockHttpMessageHandler(_ => MockHttpMessageHandler.JsonResponse(new { }));
            Services.AddScoped(_ => new HttpClient(httpHandler) { BaseAddress = new Uri("http://localhost/") });
            Services.AddScoped<AuthService>();
        }

        [Fact]
        public void Defecto1_MainLayout_SinSesionEnRaiz_DebeRedirigirALogin()
        {
            RegistrarServiciosBase();
            JSInterop.Setup<string?>("localStorage.getItem", "authToken").SetResult(null);

            var nav = Services.GetRequiredService<FakeNavigationManager>();
            nav.NavigateTo("http://localhost/");

            var cut = RenderComponent<MainLayout>(p =>
                p.Add(x => x.Body, (RenderFragment)(b => b.AddContent(0, "Panel Protegido"))));

            Assert.EndsWith("/login", nav.Uri, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(cut.FindAll(".sitel-sidebar"));
        }

        [Fact]
        public void NavMenu_Administrador_DebeMostrarTodasLasOpciones()
        {
            RegistrarServiciosBase();
            var cut = RenderComponent<NavMenu>(p => p.Add(x => x.Rol, nameof(RolUsuario.Administrador)));

            Assert.NotEmpty(cut.FindAll("[data-menu='panel']"));
            Assert.NotEmpty(cut.FindAll("[data-menu='despachos']"));
            Assert.NotEmpty(cut.FindAll("[data-menu='registrar-despacho']"));
            Assert.NotEmpty(cut.FindAll("[data-menu='inventario']"));
            Assert.NotEmpty(cut.FindAll("[data-menu='clientes']"));
            Assert.NotEmpty(cut.FindAll("[data-menu='flota']"));
            Assert.NotEmpty(cut.FindAll("[data-menu='conductor']"));
        }

        [Fact]
        public void NavMenu_OperadorLogistico_DebeOcultarFlotaYConductor()
        {
            RegistrarServiciosBase();
            var cut = RenderComponent<NavMenu>(p => p.Add(x => x.Rol, nameof(RolUsuario.OperadorLogistico)));

            Assert.NotEmpty(cut.FindAll("[data-menu='panel']"));
            Assert.NotEmpty(cut.FindAll("[data-menu='despachos']"));
            Assert.NotEmpty(cut.FindAll("[data-menu='registrar-despacho']"));
            Assert.NotEmpty(cut.FindAll("[data-menu='inventario']"));
            Assert.NotEmpty(cut.FindAll("[data-menu='clientes']"));
            Assert.Empty(cut.FindAll("[data-menu='flota']"));
            Assert.Empty(cut.FindAll("[data-menu='conductor']"));
        }

        [Fact]
        public void NavMenu_Conductor_DebeMostrarSoloVistaMovilYSeguridad()
        {
            RegistrarServiciosBase();
            var cut = RenderComponent<NavMenu>(p => p.Add(x => x.Rol, nameof(RolUsuario.Conductor)));

            Assert.Empty(cut.FindAll("[data-menu='panel']"));
            Assert.Empty(cut.FindAll("[data-menu='despachos']"));
            Assert.Empty(cut.FindAll("[data-menu='inventario']"));
            Assert.Empty(cut.FindAll("[data-menu='clientes']"));
            Assert.Empty(cut.FindAll("[data-menu='flota']"));
            Assert.NotEmpty(cut.FindAll("[data-menu='conductor']"));
            Assert.NotEmpty(cut.FindAll("[data-menu='login']"));
        }

        [Fact]
        public void Defecto2_ConductorMovil_DebeTenerRutaConductorYRenderizarTerminal()
        {
            var routeAttr = typeof(ConductorMovil)
                .GetCustomAttributes(typeof(RouteAttribute), false)
                .OfType<RouteAttribute>()
                .FirstOrDefault();

            Assert.NotNull(routeAttr);
            Assert.Equal("/conductor", routeAttr!.Template);

            var handler = new MockHttpMessageHandler(req =>
            {
                if (req.RequestUri!.AbsolutePath.Contains("api/conductores"))
                {
                    return MockHttpMessageHandler.JsonResponse(new List<Conductor>
                    {
                        new() { Id = 2, Nombres = "Carlos Andrés", Apellidos = "Mendoza Vargas", Disponible = true }
                    });
                }
                return MockHttpMessageHandler.JsonResponse(new List<Envio>());
            });

            RegistrarServiciosBase(handler);
            var cut = RenderComponent<ConductorMovil>();
            Assert.Contains("Terminal Móvil RNF02", cut.Markup);
        }

        [Fact]
        public void Defecto3_LoginPage_DebeMostrarBotonRastreo_YRastreoPublicoDebeConsultarGuiaRF04()
        {
            var handler = new MockHttpMessageHandler(req =>
            {
                if (req.RequestUri!.AbsolutePath.Contains("api/envios/rastreo/SIT-100"))
                {
                    return MockHttpMessageHandler.JsonResponse(new RastreoEnvioDto
                    {
                        Guia = "SIT-100",
                        EstadoActual = "EnRuta",
                        Origen = "Santa Cruz",
                        Destino = "Cochabamba",
                        FechaRegistro = DateTime.Now.AddHours(-3),
                        FechaEstimada = DateTime.Now.AddDays(1),
                        Historial = new List<RastreoHistorialItemDto>
                        {
                            new() { Estado = "EnRuta", Ubicacion = "Warnes", Observaciones = "En tránsito", FechaHora = DateTime.Now }
                        }
                    });
                }
                return MockHttpMessageHandler.JsonResponse(new ApiMessageResponse { Mensaje = "No encontrada" }, HttpStatusCode.NotFound);
            });

            RegistrarServiciosBase(handler);

            // 1. Verificar botón en LoginPage
            var loginCut = RenderComponent<LoginPage>();
            var btnRastreo = loginCut.Find("#btn-rastreo-publico");
            Assert.Equal("/rastreo", btnRastreo.GetAttribute("href"));

            // 2. Verificar componente RastreoPublico
            var rastreoCut = RenderComponent<RastreoPublico>();
            rastreoCut.Find("#input-guia-rastreo").Input("SIT-100");
            rastreoCut.Find("#btn-consultar-guia").Click();

            rastreoCut.WaitForAssertion(() =>
            {
                Assert.Equal("SIT-100", rastreoCut.Find("#rastreo-guia").TextContent.Trim());
                Assert.Equal("EnRuta", rastreoCut.Find("#rastreo-estado").TextContent.Trim());
            });
        }
    }
}

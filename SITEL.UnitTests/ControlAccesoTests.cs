using SITEL.Client.Services;
using SITEL.Shared.Enums;
using Xunit;

namespace SITEL.UnitTests
{
    public class ControlAccesoTests
    {
        [Theory]
        [InlineData("login", true)]
        [InlineData("/login", true)]
        [InlineData("rastreo", true)]
        [InlineData("/rastreo?guia=SIT-1", true)]
        [InlineData("", false)]
        [InlineData("mis-envios", false)]
        [InlineData("conductor", false)]
        public void EsRutaPublica_DebeIdentificarSoloLoginYRastreo(string ruta, bool esperado)
        {
            Assert.Equal(esperado, ControlAcceso.EsRutaPublica(ruta));
        }

        [Theory]
        [InlineData("", true)]
        [InlineData("mis-envios", true)]
        [InlineData("envios/nuevo", true)]
        [InlineData("inventarios", true)]
        [InlineData("clientes", true)]
        [InlineData("flota-rutas", true)]
        [InlineData("conductor", true)]
        public void PuedeAcceder_Administrador_TieneAccesoTotal(string ruta, bool esperado)
        {
            Assert.Equal(esperado, ControlAcceso.PuedeAcceder(nameof(RolUsuario.Administrador), ruta));
        }

        [Theory]
        [InlineData("", true)]
        [InlineData("mis-envios", true)]
        [InlineData("envios/nuevo", true)]
        [InlineData("inventarios", true)]
        [InlineData("clientes", true)]
        [InlineData("flota-rutas", false)]
        [InlineData("conductor", false)]
        public void PuedeAcceder_OperadorLogistico_SoloModulosOperativos(string ruta, bool esperado)
        {
            Assert.Equal(esperado, ControlAcceso.PuedeAcceder(nameof(RolUsuario.OperadorLogistico), ruta));
        }

        [Theory]
        [InlineData("conductor", true)]
        [InlineData("", false)]
        [InlineData("mis-envios", false)]
        [InlineData("inventarios", false)]
        [InlineData("clientes", false)]
        [InlineData("flota-rutas", false)]
        public void PuedeAcceder_Conductor_SoloVistaMovil(string ruta, bool esperado)
        {
            Assert.Equal(esperado, ControlAcceso.PuedeAcceder(nameof(RolUsuario.Conductor), ruta));
        }

        [Theory]
        [InlineData(nameof(RolUsuario.Administrador), "/")]
        [InlineData(nameof(RolUsuario.OperadorLogistico), "/")]
        [InlineData(nameof(RolUsuario.Conductor), "/conductor")]
        [InlineData(nameof(RolUsuario.Cliente), "/rastreo")]
        [InlineData("", "/login")]
        [InlineData(null, "/login")]
        public void RutaInicio_DebeRedirigirSegunRol(string? rol, string rutaEsperada)
        {
            Assert.Equal(rutaEsperada, ControlAcceso.RutaInicio(rol));
        }
    }
}

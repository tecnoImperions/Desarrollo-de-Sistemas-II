using SITEL.Shared.Enums;

namespace SITEL.Client.Services
{
    /// <summary>
    /// Reglas centralizadas de acceso por rol (RNF01) y filtrado del menú lateral.
    /// Clase pura (sin dependencias de UI) para permitir pruebas unitarias.
    /// </summary>
    public static class ControlAcceso
    {
        /// <summary>Rutas accesibles sin sesión iniciada.</summary>
        private static readonly string[] RutasPublicas = { "login", "rastreo" };

        /// <summary>Rutas permitidas al Operador Logístico.</summary>
        private static readonly string[] RutasOperador =
        {
            "",              // Panel Ejecutivo
            "mis-envios",    // Gestión de Despachos
            "envios/nuevo",  // Registrar Despacho
            "inventarios",   // Inventario y Carga
            "clientes"       // Directorio de Clientes
        };

        /// <summary>Rutas permitidas al Conductor (solo vista móvil RNF02).</summary>
        private static readonly string[] RutasConductor = { "conductor" };

        /// <summary>
        /// Normaliza una ruta relativa: quita query string, fragmento y barras; pasa a minúsculas.
        /// </summary>
        public static string NormalizarRuta(string? rutaRelativa)
        {
            if (string.IsNullOrWhiteSpace(rutaRelativa))
                return string.Empty;

            var ruta = rutaRelativa;
            var corte = ruta.IndexOfAny(new[] { '?', '#' });
            if (corte >= 0)
                ruta = ruta[..corte];

            return ruta.Trim().Trim('/').ToLowerInvariant();
        }

        public static bool EsRutaPublica(string? rutaRelativa)
        {
            var ruta = NormalizarRuta(rutaRelativa);
            return RutasPublicas.Any(p => ruta == p || ruta.StartsWith(p + "/", StringComparison.Ordinal));
        }

        /// <summary>
        /// Indica si un rol autenticado puede acceder a la ruta indicada.
        /// </summary>
        public static bool PuedeAcceder(string? rol, string? rutaRelativa)
        {
            var ruta = NormalizarRuta(rutaRelativa);

            if (EsRutaPublica(ruta))
                return true;

            return rol switch
            {
                nameof(RolUsuario.Administrador) => true,
                nameof(RolUsuario.OperadorLogistico) => RutasOperador.Contains(ruta),
                nameof(RolUsuario.Conductor) => RutasConductor.Contains(ruta),
                _ => false
            };
        }

        /// <summary>
        /// Ruta de inicio por defecto según el rol (usada tras el login o ante acceso denegado).
        /// </summary>
        public static string RutaInicio(string? rol) => rol switch
        {
            nameof(RolUsuario.Administrador) => "/",
            nameof(RolUsuario.OperadorLogistico) => "/",
            nameof(RolUsuario.Conductor) => "/conductor",
            nameof(RolUsuario.Cliente) => "/rastreo",
            _ => "/login"
        };
    }
}

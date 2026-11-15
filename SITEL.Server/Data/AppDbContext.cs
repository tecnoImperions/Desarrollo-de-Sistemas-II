using Microsoft.EntityFrameworkCore;
using SITEL.Shared.Entities;

namespace SITEL.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Conductor> Conductores { get; set; }
        public DbSet<Ruta> Rutas { get; set; }
        public DbSet<Envio> Envios { get; set; }
        public DbSet<HistorialEstado> HistorialEstados { get; set; }
        public DbSet<Incidencia> Incidencias { get; set; }
    }
}
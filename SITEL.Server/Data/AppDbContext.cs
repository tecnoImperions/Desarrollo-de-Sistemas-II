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
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Envio> Envios { get; set; }
        public DbSet<DetalleEnvio> DetallesEnvio { get; set; }
        public DbSet<HistorialEstado> HistorialEstados { get; set; }
        public DbSet<Incidencia> Incidencias { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Regla RN01: Guía única e inmodificable
            modelBuilder.Entity<Envio>()
                .HasIndex(e => e.Guia)
                .IsUnique();

            // Configuración de precisión decimal
            modelBuilder.Entity<Ruta>()
                .Property(r => r.DistanciaKm)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Producto>()
                .Property(p => p.PesoUnitarioKg)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Producto>()
                .HasIndex(p => p.CodigoSku)
                .IsUnique();

            // Relación Envío -> DetallesCarga
            modelBuilder.Entity<DetalleEnvio>()
                .HasOne(d => d.Envio)
                .WithMany(e => e.DetallesCarga)
                .HasForeignKey(d => d.EnvioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DetalleEnvio>()
                .HasOne(d => d.Producto)
                .WithMany()
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

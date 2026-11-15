using System;
using System.Collections.Generic;
using SITEL.Shared.Enums;

namespace SITEL.Shared.Entities
{
    public class Envio
    {
        public int Id { get; set; }
        public string Guia { get; set; } = string.Empty;

        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;

        public int ConductorId { get; set; }
        public Conductor Conductor { get; set; } = null!;

        public int RutaId { get; set; }
        public Ruta Ruta { get; set; } = null!;

        public string Destinatario { get; set; } = string.Empty;
        public string DireccionEntrega { get; set; } = string.Empty;
        public string DescripcionCarga { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public DateTime FechaEstimada { get; set; }
        public DateTime FechaEntrega { get; set; }

        public EstadoEnvio EstadoActual { get; set; } = EstadoEnvio.Registrado;
        public string ReceptorNombre { get; set; } = string.Empty;

        public bool Anulado { get; set; } = false;
        public string MotivoAnulacion { get; set; } = string.Empty;
        public string AutorizadoPor { get; set; } = string.Empty;

        public ICollection<HistorialEstado> HistorialEstados { get; set; } = new List<HistorialEstado>();
        public ICollection<Incidencia> Incidencias { get; set; } = new List<Incidencia>();
    }
}
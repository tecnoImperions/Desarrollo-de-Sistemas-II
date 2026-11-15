using System;
using SITEL.Shared.Enums;

namespace SITEL.Shared.Entities
{
    public class HistorialEstado
    {
        public int Id { get; set; }
        public int EnvioId { get; set; }
        public Envio Envio { get; set; } = null!;
        public EstadoEnvio Estado { get; set; }
        public string Ubicacion { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public string RegistradoPor { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; } = DateTime.Now;
    }
}
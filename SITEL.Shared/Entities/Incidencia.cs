using SITEL.Shared.Enums;

namespace SITEL.Shared.Entities
{
    public class Incidencia
    {
        public int Id { get; set; }
        public int EnvioId { get; set; }
        public Envio Envio { get; set; } = null!;
        public MotivoIncidencia Motivo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; } = DateTime.Now;
    }
}
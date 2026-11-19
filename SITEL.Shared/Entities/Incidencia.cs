using System;
using System.Text.Json.Serialization;
using SITEL.Shared.Enums;

namespace SITEL.Shared.Entities
{
    public class Incidencia
    {
        private string _descripcion = string.Empty;

        public int Id { get; set; }
        public int EnvioId { get; set; }

        [JsonIgnore]
        public Envio? Envio { get; set; }

        public MotivoIncidencia Motivo { get; set; }

        public string Descripcion
        {
            get => _descripcion;
            set => _descripcion = value ?? string.Empty;
        }

        public DateTime FechaHora { get; set; } = DateTime.Now;
    }
}

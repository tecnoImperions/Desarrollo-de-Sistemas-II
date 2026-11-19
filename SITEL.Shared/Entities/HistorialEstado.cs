using System;
using System.Text.Json.Serialization;
using SITEL.Shared.Enums;

namespace SITEL.Shared.Entities
{
    public class HistorialEstado
    {
        private string _ubicacion = string.Empty;
        private string _observaciones = string.Empty;
        private string _registradoPor = string.Empty;

        public int Id { get; set; }
        public int EnvioId { get; set; }

        [JsonIgnore]
        public Envio? Envio { get; set; }

        public EstadoEnvio Estado { get; set; }

        public string Ubicacion
        {
            get => _ubicacion;
            set => _ubicacion = value ?? string.Empty;
        }

        public string Observaciones
        {
            get => _observaciones;
            set => _observaciones = value ?? string.Empty;
        }

        public string RegistradoPor
        {
            get => _registradoPor;
            set => _registradoPor = value ?? string.Empty;
        }

        public DateTime FechaHora { get; set; } = DateTime.Now;
    }
}

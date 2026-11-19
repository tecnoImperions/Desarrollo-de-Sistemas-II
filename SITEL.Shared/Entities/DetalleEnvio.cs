using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SITEL.Shared.Entities
{
    public class DetalleEnvio
    {
        private string _observacionItem = string.Empty;

        public int Id { get; set; }

        public int EnvioId { get; set; }

        [JsonIgnore]
        public Envio? Envio { get; set; }

        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos 1.")]
        public int Cantidad { get; set; }

        public string ObservacionItem
        {
            get => _observacionItem;
            set => _observacionItem = value ?? string.Empty;
        }
    }
}

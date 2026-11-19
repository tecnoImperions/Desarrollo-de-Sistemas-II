namespace SITEL.Shared.Entities
{
    public class Ruta
    {
        private string _nombre = string.Empty;
        private string _origen = string.Empty;
        private string _destino = string.Empty;

        public int Id { get; set; }

        public string Nombre
        {
            get => _nombre;
            set => _nombre = value ?? string.Empty;
        }

        public string Origen
        {
            get => _origen;
            set => _origen = value ?? string.Empty;
        }

        public string Destino
        {
            get => _destino;
            set => _destino = value ?? string.Empty;
        }

        public decimal DistanciaKm { get; set; }
    }
}

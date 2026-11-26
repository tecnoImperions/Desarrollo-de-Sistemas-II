namespace SITEL.Shared.Entities
{
    public class Conductor
    {
        private string _nombres = string.Empty;
        private string _apellidos = string.Empty;
        private string _documento = string.Empty;
        private string _licencia = string.Empty;
        private string _telefono = string.Empty;

        public int Id { get; set; }

        public int? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public string Nombres
        {
            get => _nombres;
            set => _nombres = value ?? string.Empty;
        }

        public string Apellidos
        {
            get => _apellidos;
            set => _apellidos = value ?? string.Empty;
        }

        public string Documento
        {
            get => _documento;
            set => _documento = value ?? string.Empty;
        }

        public string Licencia
        {
            get => _licencia;
            set => _licencia = value ?? string.Empty;
        }

        public string Telefono
        {
            get => _telefono;
            set => _telefono = value ?? string.Empty;
        }

        public bool Disponible { get; set; } = true;
    }
}

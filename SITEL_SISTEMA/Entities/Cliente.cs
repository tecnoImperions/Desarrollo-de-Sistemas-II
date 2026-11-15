namespace SITEL.Shared.Entities
{
    public class Cliente
    {
        public int Id { get; set; }
        public string NombreRazonSocial { get; set; } = string.Empty;
        public string DocumentoNit { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
    }
}
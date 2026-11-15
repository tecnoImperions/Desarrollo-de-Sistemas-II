namespace SITEL.Shared.Entities
{
    public class Conductor
    {
        public int Id { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public string Licencia { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public bool Disponible { get; set; } = true;
    }
}
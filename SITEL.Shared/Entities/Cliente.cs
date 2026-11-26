using System.ComponentModel.DataAnnotations;

namespace SITEL.Shared.Entities
{
    public class Cliente
    {
        private string _nombreRazonSocial = string.Empty;
        private string _documentoNit = string.Empty;
        private string _correo = string.Empty;
        private string _telefono = string.Empty;
        private string _direccion = string.Empty;

        public int Id { get; set; }

        public int? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        [Required(ErrorMessage = "El nombre o razón social es obligatorio.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "La razón social debe tener entre 3 y 150 caracteres.")]
        public string NombreRazonSocial
        {
            get => _nombreRazonSocial;
            set => _nombreRazonSocial = value ?? string.Empty;
        }

        [Required(ErrorMessage = "El NIT o Documento es obligatorio.")]
        [StringLength(25, MinimumLength = 5, ErrorMessage = "El NIT/Documento debe tener entre 5 y 25 caracteres.")]
        public string DocumentoNit
        {
            get => _documentoNit;
            set => _documentoNit = value ?? string.Empty;
        }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un formato de correo electrónico válido.")]
        [StringLength(120)]
        public string Correo
        {
            get => _correo;
            set => _correo = value ?? string.Empty;
        }

        [Required(ErrorMessage = "El teléfono de contacto es obligatorio.")]
        [StringLength(30, MinimumLength = 6, ErrorMessage = "El teléfono debe tener entre 6 y 30 caracteres.")]
        public string Telefono
        {
            get => _telefono;
            set => _telefono = value ?? string.Empty;
        }

        [Required(ErrorMessage = "La dirección fiscal o de entrega es obligatoria.")]
        [StringLength(250, MinimumLength = 5, ErrorMessage = "La dirección debe tener entre 5 y 250 caracteres.")]
        public string Direccion
        {
            get => _direccion;
            set => _direccion = value ?? string.Empty;
        }
    }
}

using System.ComponentModel.DataAnnotations;
using SITEL.Shared.Enums;

namespace SITEL.Shared.Entities
{
    public class Producto
    {
        private string _codigoSku = string.Empty;
        private string _nombre = string.Empty;
        private string _descripcion = string.Empty;
        private string _unidadMedida = "Cajas";

        public int Id { get; set; }

        [Required(ErrorMessage = "El código SKU es obligatorio.")]
        [StringLength(30, ErrorMessage = "El SKU no puede superar los 30 caracteres.")]
        public string CodigoSku
        {
            get => _codigoSku;
            set => _codigoSku = value ?? string.Empty;
        }

        [Required(ErrorMessage = "El nombre del producto o mercancía es obligatorio.")]
        [StringLength(120, ErrorMessage = "El nombre no puede superar los 120 caracteres.")]
        public string Nombre
        {
            get => _nombre;
            set => _nombre = value ?? string.Empty;
        }

        [StringLength(300, ErrorMessage = "La descripción no puede superar los 300 caracteres.")]
        public string Descripcion
        {
            get => _descripcion;
            set => _descripcion = value ?? string.Empty;
        }

        [Required(ErrorMessage = "La categoría es obligatoria.")]
        public CategoriaProducto Categoria { get; set; } = CategoriaProducto.Abarrotes;

        [Required(ErrorMessage = "La unidad de medida es obligatoria.")]
        [StringLength(30)]
        public string UnidadMedida
        {
            get => _unidadMedida;
            set => _unidadMedida = string.IsNullOrWhiteSpace(value) ? "Cajas" : value;
        }

        [Range(0, int.MaxValue, ErrorMessage = "El stock actual no puede ser negativo.")]
        public int StockActual { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo.")]
        public int StockMinimo { get; set; } = 5;

        [Range(0.01, 100000, ErrorMessage = "El peso unitario debe ser mayor a 0.")]
        public decimal PesoUnitarioKg { get; set; } = 1.0m;

        public bool Activo { get; set; } = true;
    }
}

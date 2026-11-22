using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SITEL.Shared.Enums;

namespace SITEL.Shared.DTOs
{
    public class ApiMessageResponse
    {
        public string? Mensaje { get; set; }
        public string? Detalle { get; set; }
    }

    public class AjusteStockDto
    {
        private string _motivo = string.Empty;

        [Required]
        public int ProductoId { get; set; }

        [Required(ErrorMessage = "La cantidad a ajustar es obligatoria.")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "Debe indicar el motivo del movimiento de inventario.")]
        [StringLength(200)]
        public string Motivo
        {
            get => _motivo;
            set => _motivo = value ?? string.Empty;
        }
    }

    public class ItemCargaRequestDto
    {
        private string _observacionItem = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un producto válido.")]
        public int ProductoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
        public int Cantidad { get; set; }

        public string ObservacionItem
        {
            get => _observacionItem;
            set => _observacionItem = value ?? string.Empty;
        }
    }

    public class CrearEnvioRequestDto
    {
        private string _guia = string.Empty;
        private string _destinatario = string.Empty;
        private string _direccionEntrega = string.Empty;
        private string _descripcionCarga = string.Empty;
        private string _registradoPor = "Operador Logístico";
        private List<ItemCargaRequestDto> _itemsCarga = new();

        [Required(ErrorMessage = "El número de guía es obligatorio (RN01).")]
        public string Guia
        {
            get => _guia;
            set => _guia = value ?? string.Empty;
        }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un cliente destinatario válido.")]
        public int ClienteId { get; set; }

        public int ConductorId { get; set; }
        public int RutaId { get; set; }

        [Required(ErrorMessage = "El nombre del contacto destinatario es obligatorio.")]
        public string Destinatario
        {
            get => _destinatario;
            set => _destinatario = value ?? string.Empty;
        }

        [Required(ErrorMessage = "La dirección de entrega es obligatoria.")]
        public string DireccionEntrega
        {
            get => _direccionEntrega;
            set => _direccionEntrega = value ?? string.Empty;
        }

        [Required(ErrorMessage = "La descripción general de la carga es obligatoria.")]
        public string DescripcionCarga
        {
            get => _descripcionCarga;
            set => _descripcionCarga = value ?? string.Empty;
        }

        public DateTime FechaEstimada { get; set; } = DateTime.Now.AddDays(1);

        public string RegistradoPor
        {
            get => _registradoPor;
            set => _registradoPor = string.IsNullOrWhiteSpace(value) ? "Operador Logístico" : value;
        }

        public List<ItemCargaRequestDto> ItemsCarga
        {
            get => _itemsCarga;
            set => _itemsCarga = value ?? new List<ItemCargaRequestDto>();
        }
    }

    public class AsignarDespachoDto
    {
        private string _ubicacion = "Centro de Distribución Principal";
        private string _observaciones = "Asignación de unidad de transporte y hoja de ruta (RF02).";
        private string _registradoPor = "Operador Logístico";

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un conductor válido.")]
        public int ConductorId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una ruta válida.")]
        public int RutaId { get; set; }

        public DateTime? FechaEstimada { get; set; }

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
            set => _registradoPor = string.IsNullOrWhiteSpace(value) ? "Operador Logístico" : value;
        }
    }

    public class ActualizarEstadoEnvioDto
    {
        private string _ubicacion = string.Empty;
        private string _observaciones = string.Empty;
        private string _registradoPor = "Operador Logístico";
        private string _receptorNombre = string.Empty;
        private string _descripcionIncidencia = string.Empty;

        [Required]
        public EstadoEnvio NuevoEstado { get; set; }

        [Required(ErrorMessage = "La ubicación del reporte es obligatoria.")]
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
            set => _registradoPor = string.IsNullOrWhiteSpace(value) ? "Operador Logístico" : value;
        }

        public string ReceptorNombre
        {
            get => _receptorNombre;
            set => _receptorNombre = value ?? string.Empty;
        }

        public MotivoIncidencia? MotivoIncidencia { get; set; }

        public string DescripcionIncidencia
        {
            get => _descripcionIncidencia;
            set => _descripcionIncidencia = value ?? string.Empty;
        }
    }

    public class AnularEnvioDto
    {
        private string _motivoAnulacion = string.Empty;
        private string _autorizadoPor = "Administrador SITEL";

        [Required(ErrorMessage = "El motivo de anulación es obligatorio.")]
        public string MotivoAnulacion
        {
            get => _motivoAnulacion;
            set => _motivoAnulacion = value ?? string.Empty;
        }

        [Required(ErrorMessage = "Debe indicar quién autoriza la anulación.")]
        public string AutorizadoPor
        {
            get => _autorizadoPor;
            set => _autorizadoPor = string.IsNullOrWhiteSpace(value) ? "Administrador SITEL" : value;
        }
    }

    /// <summary>
    /// DTO público para consulta de rastreo por guía (RF04). No expone datos sensibles.
    /// </summary>
    public class RastreoEnvioDto
    {
        public string Guia { get; set; } = string.Empty;
        public string EstadoActual { get; set; } = string.Empty;
        public bool Anulado { get; set; }
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public DateTime FechaEstimada { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public List<RastreoHistorialItemDto> Historial { get; set; } = new();
    }

    public class RastreoHistorialItemDto
    {
        public string Estado { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
    }
}

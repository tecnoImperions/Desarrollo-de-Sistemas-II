using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SITEL.Shared.Enums;

namespace SITEL.Shared.Entities
{
    public class Envio
    {
        private string _guia = string.Empty;
        private string _destinatario = string.Empty;
        private string _direccionEntrega = string.Empty;
        private string _descripcionCarga = string.Empty;
        private string _receptorNombre = string.Empty;
        private string _motivoAnulacion = string.Empty;
        private string _autorizadoPor = string.Empty;
        private ICollection<DetalleEnvio> _detallesCarga = new List<DetalleEnvio>();
        private ICollection<HistorialEstado> _historialEstados = new List<HistorialEstado>();
        private ICollection<Incidencia> _incidencias = new List<Incidencia>();

        public int Id { get; set; }

        [Required(ErrorMessage = "El número de guía es obligatorio (RN01).")]
        public string Guia
        {
            get => _guia;
            set => _guia = value ?? string.Empty;
        }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un cliente válido.")]
        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        public int ConductorId { get; set; }
        public Conductor? Conductor { get; set; }

        public int RutaId { get; set; }
        public Ruta? Ruta { get; set; }

        [Required(ErrorMessage = "El nombre del destinatario o contacto de recepción es obligatorio.")]
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

        [Required(ErrorMessage = "La descripción de la carga es obligatoria.")]
        public string DescripcionCarga
        {
            get => _descripcionCarga;
            set => _descripcionCarga = value ?? string.Empty;
        }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public DateTime FechaEstimada { get; set; } = DateTime.Now.AddDays(1);
        public DateTime FechaEntrega { get; set; }

        public EstadoEnvio EstadoActual { get; set; } = EstadoEnvio.Registrado;

        public string ReceptorNombre
        {
            get => _receptorNombre;
            set => _receptorNombre = value ?? string.Empty;
        }

        public bool Anulado { get; set; } = false;

        public string MotivoAnulacion
        {
            get => _motivoAnulacion;
            set => _motivoAnulacion = value ?? string.Empty;
        }

        public string AutorizadoPor
        {
            get => _autorizadoPor;
            set => _autorizadoPor = value ?? string.Empty;
        }

        public ICollection<DetalleEnvio> DetallesCarga
        {
            get => _detallesCarga;
            set => _detallesCarga = value ?? new List<DetalleEnvio>();
        }

        public ICollection<HistorialEstado> HistorialEstados
        {
            get => _historialEstados;
            set => _historialEstados = value ?? new List<HistorialEstado>();
        }

        public ICollection<Incidencia> Incidencias
        {
            get => _incidencias;
            set => _incidencias = value ?? new List<Incidencia>();
        }
    }
}

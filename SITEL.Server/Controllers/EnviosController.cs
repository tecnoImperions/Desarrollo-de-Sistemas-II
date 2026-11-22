using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SITEL.Server.Data;
using SITEL.Shared.DTOs;
using SITEL.Shared.Entities;
using SITEL.Shared.Enums;

namespace SITEL.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnviosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EnviosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/envios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Envio>>> GetEnvios()
        {
            try
            {
                var envios = await _context.Envios
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(e => e.Cliente)
                    .Include(e => e.Conductor)
                    .Include(e => e.Ruta)
                    .Include(e => e.DetallesCarga)
                        .ThenInclude(d => d.Producto)
                    .Include(e => e.HistorialEstados)
                    .Include(e => e.Incidencias)
                    .OrderByDescending(e => e.FechaRegistro)
                    .ToListAsync();

                return Ok(envios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al consultar los despachos registrados.", detalle = ex.Message });
            }
        }

        // GET: api/envios/rastreo/SIT-20261122-1234 (RF04: Consulta pública de rastreo sin datos sensibles)
        [AllowAnonymous]
        [HttpGet("rastreo/{guia}")]
        public async Task<ActionResult<RastreoEnvioDto>> RastrearPorGuia(string guia)
        {
            if (string.IsNullOrWhiteSpace(guia))
                return BadRequest(new { mensaje = "Debe ingresar un número de guía válido." });

            var guiaNormalizada = guia.Trim().ToUpperInvariant();

            try
            {
                var envio = await _context.Envios
                    .AsNoTracking()
                    .Include(e => e.Ruta)
                    .Include(e => e.HistorialEstados)
                    .FirstOrDefaultAsync(e => e.Guia.ToUpper() == guiaNormalizada);

                if (envio == null)
                    return NotFound(new { mensaje = $"No se encontró ningún envío con la guía '{guiaNormalizada}'." });

                var respuesta = new RastreoEnvioDto
                {
                    Guia = envio.Guia,
                    EstadoActual = envio.Anulado ? "Anulado" : envio.EstadoActual.ToString(),
                    Anulado = envio.Anulado,
                    Origen = envio.Ruta?.Origen ?? "Almacén Central",
                    Destino = envio.Ruta?.Destino ?? "Por definir",
                    FechaRegistro = envio.FechaRegistro,
                    FechaEstimada = envio.FechaEstimada,
                    FechaEntrega = envio.FechaEntrega > DateTime.MinValue ? envio.FechaEntrega : null,
                    Historial = envio.HistorialEstados
                        .OrderByDescending(h => h.FechaHora)
                        .Select(h => new RastreoHistorialItemDto
                        {
                            Estado = h.Estado.ToString(),
                            Ubicacion = h.Ubicacion,
                            Observaciones = h.Observaciones,
                            FechaHora = h.FechaHora
                        })
                        .ToList()
                };

                return Ok(respuesta);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al consultar el rastreo del envío.", detalle = ex.Message });
            }
        }

        // GET: api/envios/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Envio>> GetEnvioPorId(int id)
        {
            try
            {
                var envio = await _context.Envios
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(e => e.Cliente)
                    .Include(e => e.Conductor)
                    .Include(e => e.Ruta)
                    .Include(e => e.DetallesCarga)
                        .ThenInclude(d => d.Producto)
                    .Include(e => e.HistorialEstados)
                    .Include(e => e.Incidencias)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (envio == null)
                    return NotFound(new { mensaje = $"No se encontró el despacho con ID {id}." });

                return Ok(envio);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener el detalle del despacho.", detalle = ex.Message });
            }
        }

        // POST: api/envios (RN01: Guía única inmodificable + Descuento Transaccional de Stock + Historial Inicial)
        [HttpPost]
        public async Task<IActionResult> CrearEnvio([FromBody] CrearEnvioRequestDto? request)
        {
            if (request == null || request.ClienteId <= 0)
                return BadRequest(new { mensaje = "Debe seleccionar un cliente destinatario válido." });

            var guiaNormalizada = string.IsNullOrWhiteSpace(request.Guia)
                ? $"SIT-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}"
                : request.Guia.Trim().ToUpperInvariant();

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Validar RN01: Guía única
                bool guiaExiste = await _context.Envios.AnyAsync(e => e.Guia.ToUpper() == guiaNormalizada);
                if (guiaExiste)
                {
                    return BadRequest(new { mensaje = $"Regla RN01: Ya existe un despacho con el número de guía '{guiaNormalizada}'." });
                }

                // 2. Validar existencia del Cliente
                var cliente = await _context.Clientes.FindAsync(request.ClienteId);
                if (cliente == null)
                {
                    return BadRequest(new { mensaje = "El cliente seleccionado no existe en el directorio." });
                }

                // 3. Resolver Conductor y Ruta (RF02 opcional en el registro o pendiente de asignación)
                int conductorId = request.ConductorId;
                int rutaId = request.RutaId;
                bool asignadoAlCrear = false;

                var conductor = conductorId > 0
                    ? await _context.Conductores.FindAsync(conductorId)
                    : await _context.Conductores.OrderBy(c => c.Id).FirstOrDefaultAsync();

                var ruta = rutaId > 0
                    ? await _context.Rutas.FindAsync(rutaId)
                    : await _context.Rutas.OrderBy(r => r.Id).FirstOrDefaultAsync();

                if (conductor == null || ruta == null)
                {
                    return BadRequest(new { mensaje = "No se encontraron registros válidos de Conductor o Ruta en el catálogo." });
                }

                if (!conductor.Nombres.Contains("Sin Asignar", StringComparison.OrdinalIgnoreCase) &&
                    !ruta.Nombre.Contains("Pendiente", StringComparison.OrdinalIgnoreCase) &&
                    request.ConductorId > 0 && request.RutaId > 0)
                {
                    asignadoAlCrear = true;
                }

                var ahora = DateTime.Now;
                var nuevoEnvio = new Envio
                {
                    Guia = guiaNormalizada,
                    ClienteId = cliente.Id,
                    ConductorId = conductor.Id,
                    RutaId = ruta.Id,
                    Destinatario = string.IsNullOrWhiteSpace(request.Destinatario) ? cliente.NombreRazonSocial : request.Destinatario.Trim(),
                    DireccionEntrega = string.IsNullOrWhiteSpace(request.DireccionEntrega) ? cliente.Direccion : request.DireccionEntrega.Trim(),
                    DescripcionCarga = request.DescripcionCarga.Trim(),
                    FechaRegistro = ahora,
                    FechaEstimada = request.FechaEstimada <= DateTime.MinValue ? ahora.AddDays(1) : request.FechaEstimada,
                    FechaEntrega = DateTime.MinValue,
                    EstadoActual = asignadoAlCrear ? EstadoEnvio.Asignado : EstadoEnvio.Registrado,
                    ReceptorNombre = string.Empty,
                    Anulado = false,
                    MotivoAnulacion = string.Empty,
                    AutorizadoPor = string.Empty
                };

                _context.Envios.Add(nuevoEnvio);
                await _context.SaveChangesAsync();

                // 4. Control Transaccional de Inventarios / Mercancía asociada al despacho
                if (request.ItemsCarga != null && request.ItemsCarga.Any())
                {
                    var itemsAgrupados = request.ItemsCarga
                        .Where(i => i != null && i.ProductoId > 0 && i.Cantidad > 0)
                        .GroupBy(i => i.ProductoId)
                        .Select(g => new
                        {
                            ProductoId = g.Key,
                            CantidadTotal = g.Sum(x => x.Cantidad),
                            Observacion = string.Join("; ", g.Select(x => x.ObservacionItem).Where(s => !string.IsNullOrWhiteSpace(s)))
                        });

                    foreach (var item in itemsAgrupados)
                    {
                        var producto = await _context.Productos.FindAsync(item.ProductoId);
                        if (producto == null)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new { mensaje = $"El producto de inventario con ID {item.ProductoId} no existe." });
                        }

                        if (!producto.Activo)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new { mensaje = $"El producto '{producto.Nombre}' se encuentra inactivo para despacho." });
                        }

                        if (producto.StockActual < item.CantidadTotal)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new
                            {
                                mensaje = $"Stock insuficiente para '{producto.Nombre}' (SKU: {producto.CodigoSku}). Disponible: {producto.StockActual} {producto.UnidadMedida}, Solicitado: {item.CantidadTotal}."
                            });
                        }

                        producto.StockActual -= item.CantidadTotal;

                        _context.DetallesEnvio.Add(new DetalleEnvio
                        {
                            EnvioId = nuevoEnvio.Id,
                            ProductoId = producto.Id,
                            Cantidad = item.CantidadTotal,
                            ObservacionItem = item.Observacion
                        });
                    }
                }

                // 5. Registrar trazabilidad inicial en HistorialEstados
                var operador = string.IsNullOrWhiteSpace(request.RegistradoPor) ? "Operador Logístico" : request.RegistradoPor.Trim();

                _context.HistorialEstados.Add(new HistorialEstado
                {
                    EnvioId = nuevoEnvio.Id,
                    Estado = EstadoEnvio.Registrado,
                    Ubicacion = "Almacén Central SITEL",
                    Observaciones = $"Orden de despacho generada con Guía única {guiaNormalizada} (RN01).",
                    RegistradoPor = operador,
                    FechaHora = ahora
                });

                if (asignadoAlCrear)
                {
                    _context.HistorialEstados.Add(new HistorialEstado
                    {
                        EnvioId = nuevoEnvio.Id,
                        Estado = EstadoEnvio.Asignado,
                        Ubicacion = ruta.Origen,
                        Observaciones = $"Asignado a conductor {conductor.Nombres} {conductor.Apellidos} en ruta '{ruta.Nombre}' (RF02).",
                        RegistradoPor = operador,
                        FechaHora = ahora.AddSeconds(1)
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetEnvioPorId), new { id = nuevoEnvio.Id }, nuevoEnvio);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { mensaje = "Error transaccional al registrar el despacho. Se ejecutó Rollback seguro.", detalle = ex.Message });
            }
        }

        // PUT: api/envios/5/asignar (RF02: Asignación o Reasignación de Conductor y Ruta con Transacción)
        [HttpPut("{id:int}/asignar")]
        public async Task<IActionResult> AsignarConductorYRuta(int id, [FromBody] AsignarDespachoDto? dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(new { mensaje = "Debe seleccionar un conductor y una ruta válidos (RF02)." });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var envio = await _context.Envios.FindAsync(id);
                if (envio == null)
                    return NotFound(new { mensaje = $"No se encontró el envío con ID {id}." });

                if (envio.Anulado)
                    return BadRequest(new { mensaje = "No se puede asignar un despacho que ha sido anulado." });

                if (envio.EstadoActual == EstadoEnvio.Entregado)
                    return BadRequest(new { mensaje = "El despacho ya fue entregado y su trazabilidad está cerrada." });

                var conductor = await _context.Conductores.FindAsync(dto.ConductorId);
                if (conductor == null)
                    return BadRequest(new { mensaje = "El conductor seleccionado no existe." });

                var ruta = await _context.Rutas.FindAsync(dto.RutaId);
                if (ruta == null)
                    return BadRequest(new { mensaje = "La ruta seleccionada no existe." });

                envio.ConductorId = conductor.Id;
                envio.RutaId = ruta.Id;
                if (dto.FechaEstimada.HasValue && dto.FechaEstimada.Value > DateTime.MinValue)
                {
                    envio.FechaEstimada = dto.FechaEstimada.Value;
                }

                if (envio.EstadoActual == EstadoEnvio.Registrado)
                {
                    envio.EstadoActual = EstadoEnvio.Asignado;
                }

                _context.HistorialEstados.Add(new HistorialEstado
                {
                    EnvioId = envio.Id,
                    Estado = envio.EstadoActual,
                    Ubicacion = string.IsNullOrWhiteSpace(dto.Ubicacion) ? ruta.Origen : dto.Ubicacion.Trim(),
                    Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones)
                        ? $"Asignación RF02: Conductor {conductor.Nombres} {conductor.Apellidos} | Ruta: {ruta.Nombre}."
                        : dto.Observaciones.Trim(),
                    RegistradoPor = string.IsNullOrWhiteSpace(dto.RegistradoPor) ? "Operador Logístico" : dto.RegistradoPor.Trim(),
                    FechaHora = DateTime.Now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    mensaje = $"Conductor '{conductor.Nombres} {conductor.Apellidos}' y ruta '{ruta.Nombre}' asignados correctamente.",
                    envioId = envio.Id,
                    estadoActual = envio.EstadoActual.ToString()
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { mensaje = "Fallo en la transacción de asignación RF02. Se revirtieron los cambios.", detalle = ex.Message });
            }
        }

        // PUT: api/envios/5/estado (RF03: Flujo de Estados + Historial + Registro de Incidencia en Transacción Atómica)
        [HttpPut("{id:int}/estado")]
        public async Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoEnvioDto? dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Ubicacion))
                return BadRequest(new { mensaje = "Debe especificar la ubicación actual para registrar el cambio de estado." });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var envio = await _context.Envios
                    .Include(e => e.Conductor)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (envio == null)
                    return NotFound(new { mensaje = $"El despacho con ID {id} no existe." });

                if (envio.Anulado)
                    return BadRequest(new { mensaje = "El despacho está anulado y no admite cambios de estado." });

                if (envio.EstadoActual == EstadoEnvio.Entregado)
                    return BadRequest(new { mensaje = "El despacho ya figura como Entregado. No se permiten cambios posteriores." });

                if ((dto.NuevoEstado == EstadoEnvio.EnRuta ||
                     dto.NuevoEstado == EstadoEnvio.EnDestino ||
                     dto.NuevoEstado == EstadoEnvio.Entregado) &&
                    (envio.Conductor != null && envio.Conductor.Nombres.Contains("Sin Asignar", StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest(new
                    {
                        mensaje = "Regla RF02: Antes de pasar el envío a 'En Ruta' o 'Entregado', debe asignar un Conductor y una Ruta."
                    });
                }

                var ahora = DateTime.Now;
                envio.EstadoActual = dto.NuevoEstado;

                if (dto.NuevoEstado == EstadoEnvio.Entregado)
                {
                    envio.FechaEntrega = ahora;
                    envio.ReceptorNombre = string.IsNullOrWhiteSpace(dto.ReceptorNombre)
                        ? envio.Destinatario
                        : dto.ReceptorNombre.Trim();
                }

                if (dto.NuevoEstado == EstadoEnvio.ConIncidencia)
                {
                    var motivo = dto.MotivoIncidencia ?? MotivoIncidencia.Otro;
                    var descIncidencia = string.IsNullOrWhiteSpace(dto.DescripcionIncidencia)
                        ? dto.Observaciones
                        : dto.DescripcionIncidencia.Trim();

                    if (string.IsNullOrWhiteSpace(descIncidencia))
                    {
                        return BadRequest(new { mensaje = "Para reportar un envío 'Con Incidencia', debe detallar el motivo o descripción del incidente." });
                    }

                    _context.Incidencias.Add(new Incidencia
                    {
                        EnvioId = envio.Id,
                        Motivo = motivo,
                        Descripcion = descIncidencia,
                        FechaHora = ahora
                    });
                }

                var observacionHistorial = dto.Observaciones?.Trim() ?? string.Empty;
                if (dto.NuevoEstado == EstadoEnvio.Entregado && !string.IsNullOrWhiteSpace(envio.ReceptorNombre))
                {
                    observacionHistorial = string.IsNullOrWhiteSpace(observacionHistorial)
                        ? $"Mercancía recibida conforme por: {envio.ReceptorNombre}."
                        : $"{observacionHistorial} (Recibido por: {envio.ReceptorNombre})";
                }
                else if (dto.NuevoEstado == EstadoEnvio.ConIncidencia)
                {
                    observacionHistorial = $"Incidencia reportada ({dto.MotivoIncidencia ?? MotivoIncidencia.Otro}): {dto.DescripcionIncidencia} {observacionHistorial}".Trim();
                }

                _context.HistorialEstados.Add(new HistorialEstado
                {
                    EnvioId = envio.Id,
                    Estado = dto.NuevoEstado,
                    Ubicacion = dto.Ubicacion.Trim(),
                    Observaciones = string.IsNullOrWhiteSpace(observacionHistorial)
                        ? $"Cambio de estado operativo a {dto.NuevoEstado}."
                        : observacionHistorial,
                    RegistradoPor = string.IsNullOrWhiteSpace(dto.RegistradoPor) ? "Operador Logístico" : dto.RegistradoPor.Trim(),
                    FechaHora = ahora
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    mensaje = $"Estado del envío {envio.Guia} actualizado a '{dto.NuevoEstado}' correctamente.",
                    envioId = envio.Id,
                    estadoActual = envio.EstadoActual.ToString()
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { mensaje = "Error transaccional al actualizar el estado (RF03). Se aplicó Rollback.", detalle = ex.Message });
            }
        }

        // PUT: api/envios/5/anular (Anulación segura con reposición automática de stock en almacén)
        [HttpPut("{id:int}/anular")]
        public async Task<IActionResult> AnularEnvio(int id, [FromBody] AnularEnvioDto? dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.MotivoAnulacion))
                return BadRequest(new { mensaje = "Debe especificar el motivo de la anulación del despacho." });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var envio = await _context.Envios
                    .Include(e => e.DetallesCarga)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (envio == null)
                    return NotFound(new { mensaje = $"No se encontró el despacho con ID {id}." });

                if (envio.Anulado)
                    return BadRequest(new { mensaje = "Este despacho ya fue anulado previamente." });

                if (envio.EstadoActual == EstadoEnvio.Entregado)
                    return BadRequest(new { mensaje = "No es posible anular un despacho que ya fue entregado en destino." });

                envio.Anulado = true;
                envio.MotivoAnulacion = dto.MotivoAnulacion.Trim();
                envio.AutorizadoPor = string.IsNullOrWhiteSpace(dto.AutorizadoPor) ? "Administrador SITEL" : dto.AutorizadoPor.Trim();

                foreach (var detalle in envio.DetallesCarga)
                {
                    var producto = await _context.Productos.FindAsync(detalle.ProductoId);
                    if (producto != null)
                    {
                        producto.StockActual += detalle.Cantidad;
                    }
                }

                _context.HistorialEstados.Add(new HistorialEstado
                {
                    EnvioId = envio.Id,
                    Estado = envio.EstadoActual,
                    Ubicacion = "Sistema Central SITEL",
                    Observaciones = $"DESPACHO ANULADO. Motivo: {envio.MotivoAnulacion}. Autorizado por: {envio.AutorizadoPor}. Stock restituido al almacén.",
                    RegistradoPor = envio.AutorizadoPor,
                    FechaHora = DateTime.Now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { mensaje = $"El despacho {envio.Guia} ha sido anulado y su mercancía fue reintegrada al stock." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { mensaje = "Error crítico al anular el despacho.", detalle = ex.Message });
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SITEL.Server.Data;
using SITEL.Shared.Entities;

namespace SITEL.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/clientes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cliente>>> GetClientes([FromQuery] string? filtro = null)
        {
            try
            {
                var query = _context.Clientes.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(filtro))
                {
                    var termino = filtro.Trim().ToLower();
                    query = query.Where(c =>
                        c.NombreRazonSocial.ToLower().Contains(termino) ||
                        c.DocumentoNit.ToLower().Contains(termino) ||
                        c.Correo.ToLower().Contains(termino));
                }

                var clientes = await query
                    .OrderBy(c => c.NombreRazonSocial)
                    .ToListAsync();

                return Ok(clientes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno al consultar el directorio de clientes.", detalle = ex.Message });
            }
        }

        // GET: api/clientes/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Cliente>> GetClientePorId(int id)
        {
            try
            {
                var cliente = await _context.Clientes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (cliente == null)
                    return NotFound(new { mensaje = $"No se encontró el cliente con ID {id}." });

                return Ok(cliente);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener los datos del cliente.", detalle = ex.Message });
            }
        }

        // POST: api/clientes
        [HttpPost]
        public async Task<ActionResult<Cliente>> CrearCliente([FromBody] Cliente cliente)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { mensaje = "Datos de cliente inválidos. Verifique los campos obligatorios." });

            cliente.NombreRazonSocial = cliente.NombreRazonSocial.Trim();
            cliente.DocumentoNit = cliente.DocumentoNit.Trim();
            cliente.Correo = cliente.Correo.Trim();
            cliente.Telefono = cliente.Telefono.Trim();
            cliente.Direccion = cliente.Direccion.Trim();

            if (string.IsNullOrWhiteSpace(cliente.NombreRazonSocial) || string.IsNullOrWhiteSpace(cliente.DocumentoNit))
                return BadRequest(new { mensaje = "La Razón Social y el NIT/Documento son campos obligatorios." });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validación estricta de duplicados por NIT / Documento
                bool existeNit = await _context.Clientes
                    .AnyAsync(c => c.DocumentoNit.ToLower() == cliente.DocumentoNit.ToLower());

                if (existeNit)
                {
                    return BadRequest(new { mensaje = $"Ya existe un cliente registrado con el NIT/Documento '{cliente.DocumentoNit}'." });
                }

                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetClientePorId), new { id = cliente.Id }, cliente);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { mensaje = "Error crítico al registrar el cliente en la base de datos.", detalle = ex.Message });
            }
        }

        // PUT: api/clientes/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> ActualizarCliente(int id, [FromBody] Cliente clienteActualizado)
        {
            if (id != clienteActualizado.Id && clienteActualizado.Id != 0)
                return BadRequest(new { mensaje = "El ID del cliente no coincide con el registro solicitado." });

            if (string.IsNullOrWhiteSpace(clienteActualizado.NombreRazonSocial) ||
                string.IsNullOrWhiteSpace(clienteActualizado.DocumentoNit))
            {
                return BadRequest(new { mensaje = "La Razón Social y el NIT/Documento son obligatorios." });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var clienteExistente = await _context.Clientes.FindAsync(id);
                if (clienteExistente == null)
                    return NotFound(new { mensaje = $"El cliente con ID {id} no existe." });

                var nitNormalizado = clienteActualizado.DocumentoNit.Trim().ToLower();
                bool nitDuplicado = await _context.Clientes
                    .AnyAsync(c => c.Id != id && c.DocumentoNit.ToLower() == nitNormalizado);

                if (nitDuplicado)
                {
                    return BadRequest(new { mensaje = $"Otro cliente ya utiliza el NIT/Documento '{clienteActualizado.DocumentoNit}'." });
                }

                clienteExistente.NombreRazonSocial = clienteActualizado.NombreRazonSocial.Trim();
                clienteExistente.DocumentoNit = clienteActualizado.DocumentoNit.Trim();
                clienteExistente.Correo = clienteActualizado.Correo.Trim();
                clienteExistente.Telefono = clienteActualizado.Telefono.Trim();
                clienteExistente.Direccion = clienteActualizado.Direccion.Trim();

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(clienteExistente);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { mensaje = "Error al actualizar la información del cliente.", detalle = ex.Message });
            }
        }

        // DELETE: api/clientes/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> EliminarCliente(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cliente = await _context.Clientes.FindAsync(id);
                if (cliente == null)
                    return NotFound(new { mensaje = $"El cliente con ID {id} no fue encontrado." });

                // Verificar integridad relacional con envíos
                bool tieneEnvios = await _context.Envios.AnyAsync(e => e.ClienteId == id);
                if (tieneEnvios)
                {
                    return BadRequest(new
                    {
                        mensaje = $"No se puede eliminar a '{cliente.NombreRazonSocial}' porque posee despachos registrados en el historial de trazabilidad."
                    });
                }

                _context.Clientes.Remove(cliente);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { mensaje = "Cliente eliminado correctamente." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { mensaje = "Error interno al intentar eliminar el cliente.", detalle = ex.Message });
            }
        }
    }
}

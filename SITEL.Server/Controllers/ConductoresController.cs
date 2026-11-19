using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SITEL.Server.Data;
using SITEL.Shared.Entities;

namespace SITEL.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConductoresController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ConductoresController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/conductores
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Conductor>>> GetConductores()
        {
            try
            {
                var conductores = await _context.Conductores
                    .AsNoTracking()
                    .OrderBy(c => c.Id)
                    .ToListAsync();

                return Ok(conductores);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener el catálogo de conductores.", detalle = ex.Message });
            }
        }

        // POST: api/conductores
        [HttpPost]
        public async Task<ActionResult<Conductor>> CrearConductor([FromBody] Conductor? conductor)
        {
            if (conductor == null ||
                string.IsNullOrWhiteSpace(conductor.Nombres) ||
                string.IsNullOrWhiteSpace(conductor.Apellidos) ||
                string.IsNullOrWhiteSpace(conductor.Documento))
            {
                return BadRequest(new { mensaje = "Nombres, apellidos y documento de identidad del conductor son obligatorios." });
            }

            conductor.Nombres = conductor.Nombres.Trim();
            conductor.Apellidos = conductor.Apellidos.Trim();
            conductor.Documento = conductor.Documento.Trim();
            conductor.Licencia = string.IsNullOrWhiteSpace(conductor.Licencia) ? "Cat. C" : conductor.Licencia.Trim();
            conductor.Telefono = conductor.Telefono.Trim();

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                bool existeDoc = await _context.Conductores
                    .AnyAsync(c => c.Documento.ToLower() == conductor.Documento.ToLower());

                if (existeDoc)
                {
                    return BadRequest(new { mensaje = $"Ya existe un conductor registrado con el documento '{conductor.Documento}'." });
                }

                _context.Conductores.Add(conductor);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetConductores), new { id = conductor.Id }, conductor);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { mensaje = "Error al registrar el conductor.", detalle = ex.Message });
            }
        }

        // PATCH: api/conductores/5/disponibilidad
        [HttpPatch("{id:int}/disponibilidad")]
        public async Task<IActionResult> CambiarDisponibilidad(int id)
        {
            var conductor = await _context.Conductores.FindAsync(id);
            if (conductor == null)
                return NotFound(new { mensaje = "Conductor no encontrado." });

            conductor.Disponible = !conductor.Disponible;
            await _context.SaveChangesAsync();

            return Ok(conductor);
        }
    }
}

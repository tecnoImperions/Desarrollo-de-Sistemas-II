using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SITEL.Server.Data;
using SITEL.Shared.Entities;

namespace SITEL.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RutasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RutasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/rutas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ruta>>> GetRutas()
        {
            try
            {
                var rutas = await _context.Rutas
                    .AsNoTracking()
                    .OrderBy(r => r.Id)
                    .ToListAsync();

                return Ok(rutas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener el catálogo de rutas.", detalle = ex.Message });
            }
        }

        // POST: api/rutas
        [HttpPost]
        public async Task<ActionResult<Ruta>> CrearRuta([FromBody] Ruta? ruta)
        {
            if (ruta == null ||
                string.IsNullOrWhiteSpace(ruta.Nombre) ||
                string.IsNullOrWhiteSpace(ruta.Origen) ||
                string.IsNullOrWhiteSpace(ruta.Destino))
            {
                return BadRequest(new { mensaje = "El nombre, punto de origen y punto de destino de la ruta son obligatorios." });
            }

            if (ruta.DistanciaKm < 0)
            {
                return BadRequest(new { mensaje = "La distancia en kilómetros no puede ser negativa." });
            }

            ruta.Nombre = ruta.Nombre.Trim();
            ruta.Origen = ruta.Origen.Trim();
            ruta.Destino = ruta.Destino.Trim();

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                bool existeRuta = await _context.Rutas
                    .AnyAsync(r => r.Nombre.ToLower() == ruta.Nombre.ToLower());

                if (existeRuta)
                {
                    return BadRequest(new { mensaje = $"Ya existe una ruta registrada con el nombre '{ruta.Nombre}'." });
                }

                _context.Rutas.Add(ruta);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetRutas), new { id = ruta.Id }, ruta);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { mensaje = "Error al registrar la ruta logística.", detalle = ex.Message });
            }
        }
    }
}

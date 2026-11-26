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
                    .Include(c => c.Usuario)
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
        public async Task<ActionResult<Conductor>> CrearConductor([FromBody] CrearConductorDto? dto)
        {
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.Nombres) ||
                string.IsNullOrWhiteSpace(dto.Apellidos) ||
                string.IsNullOrWhiteSpace(dto.Documento))
            {
                return BadRequest(new { mensaje = "Nombres, apellidos y documento de identidad del conductor son obligatorios." });
            }

            var nombres = dto.Nombres.Trim();
            var apellidos = dto.Apellidos.Trim();
            var documento = dto.Documento.Trim();
            var licencia = string.IsNullOrWhiteSpace(dto.Licencia) ? "Cat. C - Pesados" : dto.Licencia.Trim();
            var telefono = (dto.Telefono ?? string.Empty).Trim();

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                bool existeDoc = await _context.Conductores
                    .AnyAsync(c => c.Documento.ToLower() == documento.ToLower());

                if (existeDoc)
                {
                    return BadRequest(new { mensaje = $"Ya existe un conductor registrado con el documento '{documento}'." });
                }

                int? nuevoUsuarioId = null;

                // Si se proporcionó email para crear cuenta de usuario
                if (!string.IsNullOrWhiteSpace(dto.Email))
                {
                    var emailNormalizado = dto.Email.Trim().ToLower();
                    var existeUser = await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == emailNormalizado);
                    if (existeUser)
                    {
                        return BadRequest(new { mensaje = $"El correo '{dto.Email}' ya está en uso por otro usuario." });
                    }

                    var passwordPlano = string.IsNullOrWhiteSpace(dto.Password) ? "Sitel2026#" : dto.Password;
                    var nuevoUsuario = new Usuario
                    {
                        NombreCompleto = $"{nombres} {apellidos}",
                        Email = dto.Email.Trim(),
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordPlano),
                        Rol = RolUsuario.Conductor
                    };

                    _context.Usuarios.Add(nuevoUsuario);
                    await _context.SaveChangesAsync();
                    nuevoUsuarioId = nuevoUsuario.Id;
                }

                var conductor = new Conductor
                {
                    UsuarioId = nuevoUsuarioId,
                    Nombres = nombres,
                    Apellidos = apellidos,
                    Documento = documento,
                    Licencia = licencia,
                    Telefono = telefono,
                    Disponible = dto.Disponible
                };

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

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SITEL.Server.Data;
using SITEL.Shared.DTOs;
using SITEL.Shared.Entities;
using SITEL.Shared.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SITEL.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public UsuariosController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // GET: api/usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioRespuestaDto>>> GetUsuarios()
        {
            var lista = await _context.Usuarios
                .AsNoTracking()
                .Select(u => new UsuarioRespuestaDto
                {
                    Id = u.Id,
                    NombreCompleto = u.NombreCompleto,
                    Email = u.Email,
                    Rol = u.Rol,
                    ConductorId = _context.Conductores.Where(c => c.UsuarioId == u.Id).Select(c => (int?)c.Id).FirstOrDefault(),
                    ClienteId = _context.Clientes.Where(cl => cl.UsuarioId == u.Id).Select(cl => (int?)cl.Id).FirstOrDefault()
                })
                .ToListAsync();

            return Ok(lista);
        }

        // POST: api/usuarios/registro
        [HttpPost("registro")]
        public async Task<ActionResult<UsuarioRespuestaDto>> RegistrarUsuario([FromBody] RegistroUsuarioDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.PasswordHash))
            {
                return BadRequest(new { mensaje = "Email y contraseña son obligatorios." });
            }

            var existe = await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == dto.Email.Trim().ToLower());
            if (existe)
            {
                return BadRequest(new { mensaje = "El correo electrónico ya está registrado." });
            }

            var usuario = new Usuario
            {
                NombreCompleto = string.IsNullOrWhiteSpace(dto.NombreCompleto) ? dto.Email.Trim() : dto.NombreCompleto.Trim(),
                Email = dto.Email.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordHash),
                Rol = dto.Rol
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            int? conductorId = null;
            int? clienteId = null;

            // Sincronización automática de perfil operacional si es Conductor
            if (dto.Rol == RolUsuario.Conductor)
            {
                var partes = usuario.NombreCompleto.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var nombres = partes.Length > 0 ? partes[0] : "Conductor";
                var apellidos = partes.Length > 1 ? partes[1] : nombres;
                var documento = !string.IsNullOrWhiteSpace(dto.Documento)
                    ? dto.Documento.Trim()
                    : $"{new Random().Next(1000000, 9999999)} SC";

                var nuevoConductor = new Conductor
                {
                    UsuarioId = usuario.Id,
                    Nombres = nombres,
                    Apellidos = apellidos,
                    Documento = documento,
                    Licencia = !string.IsNullOrWhiteSpace(dto.Licencia) ? dto.Licencia.Trim() : "Cat. C - Pesados",
                    Telefono = !string.IsNullOrWhiteSpace(dto.Telefono) ? dto.Telefono.Trim() : "70000000",
                    Disponible = true
                };

                _context.Conductores.Add(nuevoConductor);
                await _context.SaveChangesAsync();
                conductorId = nuevoConductor.Id;
            }
            // Sincronización automática si es Cliente
            else if (dto.Rol == RolUsuario.Cliente)
            {
                var nuevoCliente = new Cliente
                {
                    UsuarioId = usuario.Id,
                    NombreRazonSocial = usuario.NombreCompleto,
                    DocumentoNit = !string.IsNullOrWhiteSpace(dto.Documento) ? dto.Documento.Trim() : $"{new Random().Next(100000000, 999999999)}",
                    Correo = usuario.Email,
                    Telefono = !string.IsNullOrWhiteSpace(dto.Telefono) ? dto.Telefono.Trim() : "70000000",
                    Direccion = "Dirección corporativa por definir"
                };

                _context.Clientes.Add(nuevoCliente);
                await _context.SaveChangesAsync();
                clienteId = nuevoCliente.Id;
            }

            var respuesta = new UsuarioRespuestaDto
            {
                Id = usuario.Id,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                Rol = usuario.Rol,
                ConductorId = conductorId,
                ClienteId = clienteId
            };

            return CreatedAtAction(nameof(GetUsuarios), new { id = usuario.Id }, respuesta);
        }

        // Sobrecarga para compatibilidad con pruebas unitarias y llamadas en código C#
        [NonAction]
        public Task<ActionResult<UsuarioRespuestaDto>> RegistrarUsuario(Usuario usuario)
        {
            if (usuario == null)
            {
                return Task.FromResult<ActionResult<UsuarioRespuestaDto>>(BadRequest(new { mensaje = "El usuario no puede ser nulo." }));
            }

            return RegistrarUsuario(new RegistroUsuarioDto
            {
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                PasswordHash = usuario.PasswordHash,
                Rol = usuario.Rol
            });
        }

        // POST: api/usuarios/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.Email.Trim().ToLower());

            // Verificar contraseña encriptada con BCrypt
            if (usuario == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, usuario.PasswordHash))
            {
                return Unauthorized(new { mensaje = "Correo o contraseña incorrectos." });
            }

            var conductor = await _context.Conductores.FirstOrDefaultAsync(c => c.UsuarioId == usuario.Id);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(cl => cl.UsuarioId == usuario.Id);

            // Generar Token JWT enriquecido con claims operacionales
            var token = GenerarTokenJwt(usuario, conductor?.Id, cliente?.Id);

            return Ok(new
            {
                mensaje = "¡Login exitoso!",
                token = token,
                usuario = new
                {
                    usuario.Id,
                    usuario.NombreCompleto,
                    usuario.Email,
                    usuario.Rol,
                    conductorId = conductor?.Id,
                    clienteId = cliente?.Id
                }
            });
        }

        private string GenerarTokenJwt(Usuario usuario, int? conductorId, int? clienteId)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Email),
                new Claim("id", usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                new Claim(ClaimTypes.Role, usuario.Rol.ToString())
            };

            if (conductorId.HasValue)
            {
                claims.Add(new Claim("conductor_id", conductorId.Value.ToString()));
            }

            if (clienteId.HasValue)
            {
                claims.Add(new Claim("cliente_id", clienteId.Value.ToString()));
            }

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
using System.Net.Http.Json;
using Microsoft.JSInterop;
using SITEL.Shared.DTOs;
using SITEL.Shared.Entities;
using SITEL.Shared.Enums;

namespace SITEL.Client.Services
{
    public class AuthService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _jsRuntime;

        public event Action? OnAuthStateChanged;

        public AuthService(HttpClient http, IJSRuntime jsRuntime)
        {
            _http = http;
            _jsRuntime = jsRuntime;
        }

        public async Task<ResultadoLoginDto> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return new ResultadoLoginDto
                {
                    Exito = false,
                    Mensaje = "Debe ingresar su correo electrónico y contraseña."
                };
            }

            try
            {
                var response = await _http.PostAsJsonAsync("api/usuarios/login", new
                {
                    Email = email.Trim(),
                    Password = password
                });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                    if (result != null && !string.IsNullOrWhiteSpace(result.Token))
                    {
                        var rolNombre = ConvertirRolATexto(result.Usuario?.Rol ?? 0);
                        var nombre = string.IsNullOrWhiteSpace(result.Usuario?.NombreCompleto)
                            ? email.Trim()
                            : result.Usuario.NombreCompleto;

                        await GuardarEnLocalStorageSeguroAsync("authToken", result.Token);
                        await GuardarEnLocalStorageSeguroAsync("userRole", rolNombre);
                        await GuardarEnLocalStorageSeguroAsync("userName", nombre);
                        await GuardarEnLocalStorageSeguroAsync("userEmail", result.Usuario?.Email ?? email.Trim());
                        if (result.Usuario != null)
                        {
                            await GuardarEnLocalStorageSeguroAsync("userId", result.Usuario.Id.ToString());
                            if (result.Usuario.ConductorId.HasValue)
                                await GuardarEnLocalStorageSeguroAsync("conductorId", result.Usuario.ConductorId.Value.ToString());
                            else
                                await EliminarDeLocalStorageSeguroAsync("conductorId");

                            if (result.Usuario.ClienteId.HasValue)
                                await GuardarEnLocalStorageSeguroAsync("clienteId", result.Usuario.ClienteId.Value.ToString());
                            else
                                await EliminarDeLocalStorageSeguroAsync("clienteId");
                        }

                        OnAuthStateChanged?.Invoke();

                        return new ResultadoLoginDto
                        {
                            Exito = true,
                            Rol = rolNombre,
                            NombreUsuario = nombre,
                            Mensaje = result.Mensaje
                        };
                    }
                }
                else
                {
                    var err = await response.Content.ReadFromJsonAsync<ApiMessageResponse>();
                    return new ResultadoLoginDto
                    {
                        Exito = false,
                        Mensaje = err?.Mensaje ?? "Credenciales incorrectas. Verifique su correo y contraseña."
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultadoLoginDto
                {
                    Exito = false,
                    Mensaje = $"No se pudo conectar con el servidor de autenticación: {ex.Message}"
                };
            }

            return new ResultadoLoginDto
            {
                Exito = false,
                Mensaje = "No se pudo validar la sesión."
            };
        }

        /// <summary>
        /// Garantiza que la cuenta corporativa exista en la base de datos y realiza el login.
        /// </summary>
        public async Task<ResultadoLoginDto> LoginAsegurandoCuentaDemoAsync(string email, string password, string nombreCompleto, RolUsuario rol)
        {
            var intentoInicial = await LoginAsync(email, password);
            if (intentoInicial.Exito)
                return intentoInicial;

            try
            {
                var nuevoUsuario = new Usuario
                {
                    NombreCompleto = nombreCompleto,
                    Email = email.Trim(),
                    PasswordHash = password,
                    Rol = rol
                };

                await _http.PostAsJsonAsync("api/usuarios/registro", nuevoUsuario);
            }
            catch
            {
                // Ignorar si ya existe
            }

            return await LoginAsync(email, password);
        }

        public async Task LogoutAsync()
        {
            await EliminarDeLocalStorageSeguroAsync("authToken");
            await EliminarDeLocalStorageSeguroAsync("userRole");
            await EliminarDeLocalStorageSeguroAsync("userName");
            await EliminarDeLocalStorageSeguroAsync("userEmail");
            await EliminarDeLocalStorageSeguroAsync("userId");
            await EliminarDeLocalStorageSeguroAsync("conductorId");
            await EliminarDeLocalStorageSeguroAsync("clienteId");

            OnAuthStateChanged?.Invoke();
        }

        public async Task<string?> GetTokenAsync()
        {
            return await LeerDeLocalStorageSeguroAsync("authToken");
        }

        public async Task<SesionActualDto> ObtenerSesionActualAsync()
        {
            var token = await LeerDeLocalStorageSeguroAsync("authToken");
            var rol = await LeerDeLocalStorageSeguroAsync("userRole");
            var nombre = await LeerDeLocalStorageSeguroAsync("userName");
            var email = await LeerDeLocalStorageSeguroAsync("userEmail");
            var userIdStr = await LeerDeLocalStorageSeguroAsync("userId");
            var condIdStr = await LeerDeLocalStorageSeguroAsync("conductorId");
            var cliIdStr = await LeerDeLocalStorageSeguroAsync("clienteId");

            int? uId = int.TryParse(userIdStr, out var p1) ? p1 : null;
            int? cId = int.TryParse(condIdStr, out var p2) ? p2 : null;
            int? clId = int.TryParse(cliIdStr, out var p3) ? p3 : null;

            return new SesionActualDto
            {
                Autenticado = !string.IsNullOrWhiteSpace(token),
                Token = token ?? string.Empty,
                UsuarioId = uId,
                ConductorId = cId,
                ClienteId = clId,
                Rol = string.IsNullOrWhiteSpace(rol) ? "OperadorLogistico" : rol,
                NombreCompleto = string.IsNullOrWhiteSpace(nombre) ? "Usuario SITEL" : nombre,
                Email = email ?? string.Empty
            };
        }

        private static string ConvertirRolATexto(int rolValor) => rolValor switch
        {
            0 => nameof(RolUsuario.Administrador),
            1 => nameof(RolUsuario.OperadorLogistico),
            2 => nameof(RolUsuario.Conductor),
            3 => nameof(RolUsuario.Cliente),
            _ => nameof(RolUsuario.OperadorLogistico)
        };

        private async Task GuardarEnLocalStorageSeguroAsync(string clave, string valor)
        {
            try { await _jsRuntime.InvokeVoidAsync("localStorage.setItem", clave, valor); }
            catch { }
        }

        private async Task<string?> LeerDeLocalStorageSeguroAsync(string clave)
        {
            try { return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", clave); }
            catch { return null; }
        }

        private async Task EliminarDeLocalStorageSeguroAsync(string clave)
        {
            try { await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", clave); }
            catch { }
        }
    }

    public class ResultadoLoginDto
    {
        public bool Exito { get; set; }
        public string Rol { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;

        public static implicit operator bool(ResultadoLoginDto? resultado) => resultado?.Exito ?? false;
    }

    public class SesionActualDto
    {
        public bool Autenticado { get; set; }
        public string Token { get; set; } = string.Empty;
        public int? UsuarioId { get; set; }
        public int? ConductorId { get; set; }
        public int? ClienteId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Mensaje { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public UsuarioDto? Usuario { get; set; }
    }

    public class UsuarioDto
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Rol { get; set; }
        public int? ConductorId { get; set; }
        public int? ClienteId { get; set; }
    }
}

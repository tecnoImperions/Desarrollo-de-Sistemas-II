using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SITEL.Client;
using SITEL.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 1. Determinar dinámicamente la URL del backend SITEL.Server
var clientBaseAddress = builder.HostEnvironment.BaseAddress;
string apiBaseUrl = clientBaseAddress;

if (clientBaseAddress.Contains("localhost:7046", StringComparison.OrdinalIgnoreCase))
{
    apiBaseUrl = "https://localhost:7247/";
}
else if (clientBaseAddress.Contains("localhost:5110", StringComparison.OrdinalIgnoreCase))
{
    apiBaseUrl = "http://localhost:5281/";
}

// 2. IMPORTANTE: Registrar TokenHandler como Transient (requerido por IHttpClientFactory para DelegatingHandlers)
builder.Services.AddTransient<TokenHandler>();

// 3. Configurar el HttpClient principal "SitelAPI" con TokenHandler
builder.Services.AddHttpClient("SitelAPI", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<TokenHandler>();

// 4. Exponer el cliente "SitelAPI" como HttpClient por defecto para todos los componentes Blazor
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("SitelAPI"));

// 5. Registrar el servicio de autenticación
builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();

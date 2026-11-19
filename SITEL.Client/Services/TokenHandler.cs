using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace SITEL.Client.Services
{
    public class TokenHandler : DelegatingHandler
    {
        private readonly IJSRuntime _jsRuntime;

        public TokenHandler(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "authToken");
                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
            catch
            {
                // Ignorar si el contexto JS aún no está disponible
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}

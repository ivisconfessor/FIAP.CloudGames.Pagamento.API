using System.Net.Http.Headers;

namespace FIAP.CloudGames.Pagamento.API.Infrastructure.Services;

public class ForwardAuthTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ForwardAuthTokenHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null && context.Request.Headers.TryGetValue("Authorization", out var authValues))
            {
                var auth = authValues.ToString();
                if (!string.IsNullOrWhiteSpace(auth))
                {
                    // Remove any existing header and add the incoming one
                    request.Headers.Remove("Authorization");
                    request.Headers.Authorization = AuthenticationHeaderValue.Parse(auth);
                }
            }
        }
        catch
        {
            // Silently ignore — se não for possível encaminhar, a chamada seguirá sem token
        }

        return base.SendAsync(request, cancellationToken);
    }
}

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TekkenFrameData.Library.Exstensions;

public static class HttpContextExtensions
{
    public static async Task<AuthenticationScheme[]> GetExternalProvidersAsync(
        this HttpContext context
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        var schemes = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();

        var list = new List<AuthenticationScheme>();
        foreach (var scheme in (await schemes.GetAllSchemesAsync()))
        {
            if (!string.IsNullOrEmpty(scheme.DisplayName))
            {
                list.Add(scheme);
            }
        }

        return [.. list];
    }

    public static async Task<bool> IsProviderSupportedAsync(
        this HttpContext context,
        string provider
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        return (
            from scheme in await context.GetExternalProvidersAsync()
            where string.Equals(scheme.Name, provider, StringComparison.OrdinalIgnoreCase)
            select scheme
        ).Any();
    }
}

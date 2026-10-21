using Microsoft.Extensions.Configuration;
using qckdev.AspNetCore.ForwardedHeaders;
using qckdev.AspNetCore.Mvc.Filters.IpSafe;
using System.Threading;
using System.Threading.Tasks;

namespace IpSafeExample.Services
{
    // Replace the configuration reads with a database, Key Vault, or another service as needed.
    sealed class AppSecuritySettingsService : IForwardedHeadersSettingsProvider, IIpSafeSettingsProvider
    {
        readonly IConfiguration _configuration;

        public AppSecuritySettingsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        Task<ForwardedHeadersSettings?> IForwardedHeadersSettingsProvider.GetSettingsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<ForwardedHeadersSettings?>(_configuration.GetSection("Security:ForwardedHeaders")
                .Get<ForwardedHeadersSettings>());
        }

        Task<IpSafeListSettings?> IIpSafeSettingsProvider.GetSettingsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IpSafeListSettings?>(_configuration.GetSection("Security:IpSafeList")
                .Get<IpSafeListSettings>());
        }
    }
}

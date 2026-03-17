using qckdev.AspNetCore.Mvc.Filters.IpSafe;
using System.Threading;
using System.Threading.Tasks;

namespace IpSafeExample.Services
{
    sealed class ServiceTrustedProxiesProvider : IIpSafeTrustedProxiesProvider
    {
        readonly IAppSecuritySettingsService _settingsService;

        public ServiceTrustedProxiesProvider(IAppSecuritySettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public async Task<IpSafeTrustedProxiesSettings?> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            var settings = await _settingsService.GetIpSafeSettingsAsync(cancellationToken);
            return new IpSafeTrustedProxiesSettings
            {
                KnownProxies = settings.KnownProxies,
                KnownNetworks = settings.TrustedKnownNetworks
            };
        }
    }
}

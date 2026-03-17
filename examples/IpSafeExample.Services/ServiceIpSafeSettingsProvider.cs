using qckdev.AspNetCore.Mvc.Filters.IpSafe;
using System.Threading;
using System.Threading.Tasks;

namespace IpSafeExample.Services
{
    sealed class ServiceIpSafeSettingsProvider : IIpSafeSettingsProvider
    {
        readonly IAppSecuritySettingsService _settingsService;

        public ServiceIpSafeSettingsProvider(IAppSecuritySettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public async Task<IpSafeListSettings?> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            var settings = await _settingsService.GetIpSafeSettingsAsync(cancellationToken);
            return new IpSafeListSettings
            {
                IpAddresses = settings.IpAddresses,
                IpNetworks = settings.IpNetworks,
                KnownProxies = settings.KnownProxies
            };
        }
    }
}

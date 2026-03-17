using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace IpSafeExample.Services
{
    sealed class AppSecuritySettingsService : IAppSecuritySettingsService
    {
        readonly IOptionsMonitor<IpSafeServiceSettings> _options;

        public AppSecuritySettingsService(IOptionsMonitor<IpSafeServiceSettings> options)
        {
            _options = options;
        }

        public Task<IpSafeServiceSettings> GetIpSafeSettingsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_options.CurrentValue ?? new IpSafeServiceSettings());
        }
    }
}

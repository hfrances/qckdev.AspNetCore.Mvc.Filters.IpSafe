using System.Threading;
using System.Threading.Tasks;

namespace IpSafeExample.Services
{
    interface IAppSecuritySettingsService
    {
        Task<IpSafeServiceSettings> GetIpSafeSettingsAsync(CancellationToken cancellationToken = default);
    }
}

using qckdev.AspNetCore.Mvc.Filters.IpSafe;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class QIpSafeDependencyInjection
    {

        public static IServiceCollection AddIpSafeFilter(this IServiceCollection services, IpSafeListSettings settings)
        {

            if (settings != null)
            {
                services.Configure<IpSafeListSettings>(config =>
                {
                    config.IpAddresses = settings?.IpAddresses;
                    config.IpNetworks = settings?.IpNetworks;
                });
            }
            return services;
        }

    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe.Test
{
    /// <summary>
    /// Verifies service-lifetime behavior for <see cref="IIpSafeSettingsProvider"/> when the middleware is configured.
    /// </summary>
    [TestClass]
    public class ScopedProviderResolutionTests
    {
        /// <summary>
        /// Ensures that app startup does not fail when the settings provider is registered as scoped.
        /// Expected result: host can be built without throwing.
        /// </summary>
        [TestMethod]
        public void UseIpSafeFilter_WithScopedProvider_DoesNotThrow()
        {
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://localhost:0");
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddControllers();
                        services.AddIpSafeFilter<TestScopedSettingsProvider>();
                        services.Configure<IpSafeListSettings>(options =>
                        {
                            options.IpAddresses = "127.0.0.1;::1";
                            options.KnownProxies = string.Empty;
                        });
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseIpSafeFilter();
                        app.UseEndpoints(_ => { });
                    });
                });

            using var host = hostBuilder.Build();
        }

        sealed class TestScopedSettingsProvider : IIpSafeSettingsProvider
        {
            public Task<IpSafeListSettings?> GetSettingsAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IpSafeListSettings?>(new IpSafeListSettings
                {
                    IpAddresses = "127.0.0.1;::1",
                    KnownProxies = string.Empty
                });
            }
        }
    }
}

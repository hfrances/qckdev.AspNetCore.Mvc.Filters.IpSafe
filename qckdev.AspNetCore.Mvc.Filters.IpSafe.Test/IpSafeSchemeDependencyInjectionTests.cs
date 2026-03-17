using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe.Test
{
    /// <summary>
    /// Verifies named-scheme registration and resolution for IpSafe settings.
    /// </summary>
    [TestClass]
    public class IpSafeSchemeDependencyInjectionTests
    {
        /// <summary>
        /// Ensures that the default options-based provider resolves named scheme settings.
        /// Expected result: configured named scheme values are returned.
        /// </summary>
        [TestMethod]
        public void AddIpSafeFilter_WithNamedSchemeAndDefaultProvider_ResolvesNamedSettings()
        {
            var services = new ServiceCollection();
            services.AddIpSafeFilter<IpSafeSettingsProvider>();
            services.AddIpSafeFilter("Internal", options =>
            {
                options.IpAddresses = "203.0.113.10";
                options.IpNetworks = "10.0.0.0/24";
                options.KnownProxies = "127.0.0.1";
            });

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var schemeProvider = scope.ServiceProvider.GetRequiredService<IIpSafeSettingsProvider>() as IIpSafeSchemeSettingsProvider;

            Assert.IsNotNull(schemeProvider);

            var settings = schemeProvider.GetSettingsAsync("Internal").GetAwaiter().GetResult();
            Assert.IsNotNull(settings);
            Assert.AreEqual("203.0.113.10", settings.IpAddresses);
            Assert.AreEqual("10.0.0.0/24", settings.IpNetworks);
            Assert.AreEqual("127.0.0.1", settings.KnownProxies);
        }
    }
}

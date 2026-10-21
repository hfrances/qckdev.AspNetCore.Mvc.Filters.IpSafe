using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

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
                options.Configure(settings =>
                {
                    settings.IpAddresses = "203.0.113.10";
                    settings.IpNetworks = "10.0.0.0/24";
                    settings.KnownProxies = "127.0.0.1";
                }));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var schemeProvider = scope.ServiceProvider.GetRequiredService<IIpSafeSettingsProvider>() as IIpSafeSchemeSettingsProvider;

            Assert.IsNotNull(schemeProvider);

            var settings = schemeProvider.GetSettingsAsync("Internal").GetAwaiter().GetResult();
            Assert.IsNotNull(settings);
            Assert.AreEqual("203.0.113.10", settings.IpAddresses!.ToString());
            Assert.AreEqual("10.0.0.0/24", settings.IpNetworks!.ToString());
            Assert.AreEqual("127.0.0.1", settings.KnownProxies!.ToString());
        }

        [TestMethod]
        public void AddIpSafeFilter_FromConfigurationAcceptsArraysAndDelimitedStrings()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["IpSafe:IpAddresses:0"] = "203.0.113.10",
                    ["IpSafe:IpAddresses:1"] = "198.51.100.20",
                    ["IpSafe:IpNetworks"] = "192.0.2.0/24; 2001:db8::/32",
                    ["IpSafe:KnownProxies:0"] = "10.0.0.10",
                    ["IpSafe:KnownProxies:1"] = "10.0.0.11"
                })
                .Build();
            var services = new ServiceCollection();
            services.AddIpSafeFilter<IpSafeSettingsProvider>(configuration, "IpSafe");
            services.AddIpSafeFilter("Internal", options =>
                options.WithConfiguration(configuration, "IpSafe"));

            using var provider = services.BuildServiceProvider();
            var settings = provider.GetRequiredService<IOptionsMonitor<IpSafeListSettings>>().CurrentValue;

            Assert.AreEqual("203.0.113.10;198.51.100.20", settings.IpAddresses!.ToString());
            Assert.AreEqual("192.0.2.0/24;2001:db8::/32", settings.IpNetworks!.ToString());
            Assert.AreEqual("10.0.0.10;10.0.0.11", settings.KnownProxies!.ToString());

            using var scope = provider.CreateScope();
            var schemeProvider = scope.ServiceProvider.GetRequiredService<IIpSafeSettingsProvider>() as IIpSafeSchemeSettingsProvider;
            var schemeSettings = schemeProvider!.GetSettingsAsync("Internal").GetAwaiter().GetResult();

            Assert.AreEqual("203.0.113.10;198.51.100.20", schemeSettings!.IpAddresses!.ToString());
            Assert.AreEqual("192.0.2.0/24;2001:db8::/32", schemeSettings.IpNetworks!.ToString());
            Assert.AreEqual("10.0.0.10;10.0.0.11", schemeSettings.KnownProxies!.ToString());
        }
    }
}

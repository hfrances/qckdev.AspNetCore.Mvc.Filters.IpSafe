using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe.Test
{
    [TestClass]
    public class IpSafeOptionsBuilderTests
    {
        [TestMethod]
        public void WithConfiguration_ReloadsDefaultAndNamedRulesIndependently()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Default:IpAddresses"] = "127.0.0.1",
                    ["Office:IpAddresses"] = "198.51.100.10",
                    ["Office:IpNetworks"] = "192.0.2.0/24",
                    ["Partner:IpAddresses"] = "198.51.100.20"
                })
                .Build();
            var services = new ServiceCollection();
            var result = services.AddIpSafeFilter<IpSafeSettingsProvider>(options =>
                options.WithConfiguration(configuration, "Default")
                    .Configure(settings => settings.IpNetworks = null));
            services.AddIpSafeFilter("Office", options =>
            {
                Assert.AreEqual("Office", options.Name);
                Assert.AreSame(options, options.WithConfiguration(configuration, "Office"));
            });
            services.AddIpSafeFilter("Partner", options => options.WithConfiguration(configuration, "Partner"));
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var settingsProvider = (IIpSafeSchemeSettingsProvider)scope.ServiceProvider.GetRequiredService<IIpSafeSettingsProvider>();
            var initialOffice = settingsProvider.GetSettingsAsync("Office").GetAwaiter().GetResult();

            Assert.AreSame(services, result);
            Assert.AreEqual("198.51.100.10", initialOffice!.IpAddresses!.ToString());
            Assert.AreEqual("192.0.2.0/24", initialOffice.IpNetworks!.ToString());
            configuration["Office:IpAddresses"] = "198.51.100.11";
            configuration.Reload();

            var office = settingsProvider.GetSettingsAsync("Office").GetAwaiter().GetResult();
            var partner = settingsProvider.GetSettingsAsync("Partner").GetAwaiter().GetResult();
            var defaults = settingsProvider.GetSettingsAsync().GetAwaiter().GetResult();
            Assert.AreNotSame(initialOffice, office);
            Assert.AreEqual("198.51.100.11", office!.IpAddresses!.ToString());
            Assert.AreEqual("198.51.100.20", partner!.IpAddresses!.ToString());
            Assert.AreEqual("127.0.0.1", defaults!.IpAddresses!.ToString());
            Assert.IsNull(defaults.IpNetworks);

            configuration["Default:IpAddresses"] = "127.0.0.2";
            configuration.Reload();

            Assert.AreEqual("127.0.0.2", settingsProvider.GetSettingsAsync().GetAwaiter().GetResult()!.IpAddresses!.ToString());
            Assert.AreEqual("198.51.100.11", settingsProvider.GetSettingsAsync("Office").GetAwaiter().GetResult()!.IpAddresses!.ToString());
            Assert.AreEqual("198.51.100.20", settingsProvider.GetSettingsAsync("Partner").GetAwaiter().GetResult()!.IpAddresses!.ToString());
        }

        [TestMethod]
        public void Configure_SupportsInlineDefaultAndNamedRules()
        {
            var services = new ServiceCollection();
            services.AddIpSafeFilter<IpSafeSettingsProvider>(options =>
                options.Configure(settings => settings.IpAddresses = "127.0.0.1"));
            services.AddIpSafeFilter("Office", options =>
                options.Configure(settings => settings.IpAddresses = "198.51.100.10"));
            using var provider = services.BuildServiceProvider();
            var monitor = provider.GetRequiredService<IOptionsMonitor<IpSafeListSettings>>();

            Assert.AreEqual("127.0.0.1", monitor.CurrentValue.IpAddresses!.ToString());
            Assert.AreEqual("198.51.100.10", monitor.Get("Office").IpAddresses!.ToString());
        }

        [TestMethod]
        public void EmptyCallbacks_AreUnambiguousAndResolveDefaultOptions()
        {
            var services = new ServiceCollection();
            services.AddIpSafeFilter<IpSafeSettingsProvider>(options => { });
            services.AddIpSafeFilter("Empty", options => { });
            using var provider = services.BuildServiceProvider();
            var monitor = provider.GetRequiredService<IOptionsMonitor<IpSafeListSettings>>();

            Assert.IsNull(monitor.CurrentValue.IpAddresses);
            Assert.IsNull(monitor.Get("Empty").IpAddresses);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void WithConfiguration_InvalidSection_Throws(string? sectionName)
        {
            var options = new ServiceCollection().AddOptions<IpSafeListSettings>();
            var configuration = new ConfigurationBuilder().Build();

            Assert.ThrowsException<ArgumentException>(() => options.WithConfiguration(configuration, sectionName!));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void NamedRegistration_InvalidScheme_Throws(string? scheme)
        {
            var services = new ServiceCollection();

            Assert.ThrowsException<ArgumentException>(() => services.AddIpSafeFilter(scheme!, options => { }));
            Assert.AreEqual(0, services.Count);
        }

        [TestMethod]
        public void RegistrationAndBinding_NullArguments_Throw()
        {
            var services = new ServiceCollection();
            var options = services.AddOptions<IpSafeListSettings>();
            var configuration = new ConfigurationBuilder().Build();

            Assert.ThrowsException<ArgumentNullException>(() =>
                IpSafeOptionsBuilderExtensions.WithConfiguration(null!, configuration, "IpSafe"));
            Assert.ThrowsException<ArgumentNullException>(() => options.WithConfiguration(null!, "IpSafe"));
            Assert.ThrowsException<ArgumentNullException>(() => services.AddIpSafeFilter<IpSafeSettingsProvider>(null!));
            Assert.ThrowsException<ArgumentNullException>(() => services.AddIpSafeFilter(
                "Office", (Action<OptionsBuilder<IpSafeListSettings>>)null!));
            Assert.ThrowsException<ArgumentNullException>(() =>
                QIpSafeDependencyInjection.AddIpSafeFilter<IpSafeSettingsProvider>(null!, builder => { }));
            Assert.ThrowsException<ArgumentNullException>(() =>
                QIpSafeDependencyInjection.AddIpSafeFilter(null!, "Office", builder => { }));
        }
    }
}

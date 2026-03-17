using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe.Test
{
    /// <summary>
    /// Verifies that Forwarded Headers trust configuration prevents IP spoofing through X-Forwarded-For.
    /// </summary>
    [TestClass]
    public class IpSafeForwardedHeadersSecurityTests
    {
        /// <summary>
        /// Ensures that X-Forwarded-For is ignored when the caller proxy is not trusted.
        /// Expected result: request is forbidden because loopback is not in the allowed IP list.
        /// </summary>
        [TestMethod]
        public void UntrustedProxy_XForwardedForIsIgnored_ReturnsForbidden()
        {
            var statusCode = ExecuteProtectedRequestWithForwardedFor(
                trustLoopbackProxy: false,
                forwardedForIp: "203.0.113.10");

            Assert.AreEqual(HttpStatusCode.Forbidden, statusCode);
        }

        /// <summary>
        /// Ensures that X-Forwarded-For is honored when the caller proxy is explicitly trusted.
        /// Expected result: request is allowed because forwarded IP is in the allowed list.
        /// </summary>
        [TestMethod]
        public void TrustedProxy_XForwardedForIsApplied_ReturnsOk()
        {
            var statusCode = ExecuteProtectedRequestWithForwardedFor(
                trustLoopbackProxy: true,
                forwardedForIp: "203.0.113.10");

            Assert.AreEqual(HttpStatusCode.OK, statusCode);
        }

        /// <summary>
        /// Ensures that trusted proxies can be configured through UseIpSafeFilter with a provider.
        /// Expected result: request is allowed when provider trusts loopback proxy.
        /// </summary>
        [TestMethod]
        public void TrustedProxyProvider_XForwardedForIsApplied_ReturnsOk()
        {
            using var host = BuildHostWithBuilderUsingProxyService<LoopbackTrustedProxiesProvider>();
            var statusCode = ExecuteProtectedRequestWithForwardedFor(host, "203.0.113.10");

            Assert.AreEqual(HttpStatusCode.OK, statusCode);
        }

        /// <summary>
        /// Ensures that untrusted proxies configured through a provider do not allow spoofing.
        /// Expected result: request is forbidden when loopback proxy is not trusted.
        /// </summary>
        [TestMethod]
        public void UntrustedProxyProvider_XForwardedForIsIgnored_ReturnsForbidden()
        {
            using var host = BuildHostWithBuilderUsingProxyService<UntrustedProxyProvider>();
            var statusCode = ExecuteProtectedRequestWithForwardedFor(host, "203.0.113.10");

            Assert.AreEqual(HttpStatusCode.Forbidden, statusCode);
        }

        /// <summary>
        /// Ensures that fluent UseIpSafeFilter builder can load trusted proxies from a provider.
        /// Expected result: request is allowed when provider trusts loopback proxy.
        /// </summary>
        [TestMethod]
        public void BuilderWithProxyService_XForwardedForIsApplied_ReturnsOk()
        {
            using var host = BuildHostWithBuilderUsingProxyService<LoopbackTrustedProxiesProvider>();
            var statusCode = ExecuteProtectedRequestWithForwardedFor(host, "203.0.113.10");

            Assert.AreEqual(HttpStatusCode.OK, statusCode);
        }

        /// <summary>
        /// Ensures that fluent UseIpSafeFilter builder can set known proxies manually.
        /// Expected result: request is allowed when loopback proxy is added via builder.
        /// </summary>
        [TestMethod]
        public void BuilderWithKnownProxies_XForwardedForIsApplied_ReturnsOk()
        {
            using var host = BuildHostWithBuilderKnownProxies("127.0.0.1");
            var statusCode = ExecuteProtectedRequestWithForwardedFor(host, "203.0.113.10");

            Assert.AreEqual(HttpStatusCode.OK, statusCode);
        }

        /// <summary>
        /// Ensures that fluent UseIpSafeFilter builder can bind trusted proxies/networks from IConfiguration.
        /// Expected result: request is allowed when configuration contains loopback in KnownProxies.
        /// </summary>
        [TestMethod]
        public void BuilderWithConfigurationBinding_XForwardedForIsApplied_ReturnsOk()
        {
            using var host = BuildHostWithBuilderConfigurationBinding();
            var statusCode = ExecuteProtectedRequestWithForwardedFor(host, "203.0.113.10");

            Assert.AreEqual(HttpStatusCode.OK, statusCode);
        }

        static HttpStatusCode ExecuteProtectedRequestWithForwardedFor(bool trustLoopbackProxy, string forwardedForIp)
        {
            using var host = BuildHost(trustLoopbackProxy);
            return ExecuteProtectedRequestWithForwardedFor(host, forwardedForIp);
        }

        static HttpStatusCode ExecuteProtectedRequestWithForwardedFor(IHost host, string forwardedForIp)
        {
            host.Start();

            var addressFeature = host.Services
                .GetRequiredService<IServer>()
                .Features
                .Get<IServerAddressesFeature>();
            var address = addressFeature
                ?.Addresses
                .Single();
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new InvalidOperationException("Could not resolve test server address.");
            }

            using var client = new HttpClient { BaseAddress = new Uri(address) };
            using var request = new HttpRequestMessage(HttpMethod.Get, "ipsafe/protected");
            request.Headers.Add("X-Forwarded-For", forwardedForIp);
            request.Headers.Add("X-Forwarded-Proto", "http");

            using var response = client.SendAsync(request).GetAwaiter().GetResult();
            return response.StatusCode;
        }

        static IHost BuildHost(bool trustLoopbackProxy)
        {
            return Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://127.0.0.1:0");
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddControllers().AddApplicationPart(typeof(IpSafeTestController).Assembly);
                        services.AddIpSafeFilter<IpSafeSettingsProvider>();
                        services.Configure<IpSafeListSettings>(options =>
                        {
                            options.IpAddresses = "203.0.113.10";
                            options.KnownProxies = string.Empty;
                        });
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseIpSafeFilter(options =>
                        {
#if NET10_0_OR_GREATER
                            options.KnownIPNetworks.Clear();
#else
                            options.KnownNetworks.Clear();
#endif
                            options.KnownProxies.Clear();
                            if (trustLoopbackProxy)
                            {
                                options.KnownProxies.Add(IPAddress.Loopback);
                            }
                            else
                            {
                                options.KnownProxies.Add(IPAddress.Parse("10.1.1.1"));
                            }

                            options.ForwardLimit = 1;
                            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                        });
                        app.UseEndpoints(endpoints => endpoints.MapControllers());
                    });
                })
                .Build();
        }

        static IHost BuildHostWithBuilderUsingProxyService<TTrustedProxiesProvider>()
            where TTrustedProxiesProvider : class, IIpSafeTrustedProxiesProvider
        {
            return Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://127.0.0.1:0");
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddControllers().AddApplicationPart(typeof(IpSafeTestController).Assembly);
                        services.AddIpSafeFilter<IpSafeSettingsProvider>();
                        services.Configure<IpSafeListSettings>(options =>
                        {
                            options.IpAddresses = "203.0.113.10";
                            options.KnownProxies = string.Empty;
                        });
                        services.AddScoped<TTrustedProxiesProvider>();
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseIpSafeFilter(cfg =>
                        {
                            cfg.WithProxyService<TTrustedProxiesProvider>();
                        });
                        app.UseEndpoints(endpoints => endpoints.MapControllers());
                    });
                })
                .Build();
        }

        static IHost BuildHostWithBuilderKnownProxies(params string[] knownProxies)
        {
            return Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://127.0.0.1:0");
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddControllers().AddApplicationPart(typeof(IpSafeTestController).Assembly);
                        services.AddIpSafeFilter<IpSafeSettingsProvider>();
                        services.Configure<IpSafeListSettings>(options =>
                        {
                            options.IpAddresses = "203.0.113.10";
                            options.KnownProxies = string.Empty;
                        });
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseIpSafeFilter(cfg =>
                        {
                            cfg.WithKnownProxies(knownProxies);
                        });
                        app.UseEndpoints(endpoints => endpoints.MapControllers());
                    });
                })
                .Build();
        }

        static IHost BuildHostWithBuilderConfigurationBinding()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["IpSafeList:TrustedForwardedHeaders:KnownProxies"] = "127.0.0.1",
                        ["IpSafeList:TrustedForwardedHeaders:KnownNetworks"] = string.Empty
                    });
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://127.0.0.1:0");
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddControllers().AddApplicationPart(typeof(IpSafeTestController).Assembly);
                        services.AddIpSafeFilter<IpSafeSettingsProvider>();
                        services.Configure<IpSafeListSettings>(options =>
                        {
                            options.IpAddresses = "203.0.113.10";
                            options.KnownProxies = string.Empty;
                        });
                    });
                    webBuilder.Configure((context, app) =>
                    {
                        app.UseRouting();
                        app.UseIpSafeFilter(cfg =>
                        {
                            cfg.WithConfiguration<IpSafeTrustedProxiesSettings>(context.Configuration, (config, target) =>
                                config.GetSection("IpSafeList:TrustedForwardedHeaders").Bind(target));
                        });
                        app.UseEndpoints(endpoints => endpoints.MapControllers());
                    });
                })
                .Build();
        }

        sealed class LoopbackTrustedProxiesProvider : IIpSafeTrustedProxiesProvider
        {
            public Task<IpSafeTrustedProxiesSettings?> GetSettingsAsync(System.Threading.CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IpSafeTrustedProxiesSettings?>(new IpSafeTrustedProxiesSettings
                {
                    KnownProxies = "127.0.0.1",
                    KnownNetworks = string.Empty
                });
            }
        }

        sealed class UntrustedProxyProvider : IIpSafeTrustedProxiesProvider
        {
            public Task<IpSafeTrustedProxiesSettings?> GetSettingsAsync(System.Threading.CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IpSafeTrustedProxiesSettings?>(new IpSafeTrustedProxiesSettings
                {
                    KnownProxies = "10.1.1.1",
                    KnownNetworks = string.Empty
                });
            }
        }
    }
}

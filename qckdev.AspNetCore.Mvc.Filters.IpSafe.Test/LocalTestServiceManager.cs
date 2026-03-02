using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using qckdev.AspNetCore.Mvc.Filters.IpSafe;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe.Test
{
    internal static class LocalTestServiceManager
    {
        private static readonly object SyncLock = new object();
        private static IHost? Host;
        private static bool Initialized;
        private static readonly Uri BaseUri = new Uri($"http://localhost:{GetPortForCurrentProcess()}/");

        public static Uri ServiceUri => BaseUri;

        public static void StartIfNeeded()
        {
            lock (SyncLock)
            {
                if (Initialized)
                {
                    return;
                }

                var builder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseUrls(BaseUri.ToString());
                        webBuilder.ConfigureServices(services =>
                        {
                            services.AddControllers().AddApplicationPart(typeof(IpSafeTestController).Assembly);
                            services.AddIpSafeFilter<IpSafeSettingsProvider>();
                            services.Configure<IpSafeListSettings>(options =>
                            {
                                options.IpAddresses = "127.0.0.1;::1";
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
                                options.KnownProxies.Add(IPAddress.Loopback);
                                options.KnownProxies.Add(IPAddress.IPv6Loopback);
                                options.ForwardLimit = 1;
                                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                            });
                            app.UseEndpoints(endpoints =>
                            {
                                endpoints.MapControllers();
                            });
                        });
                    });

                Host = builder.Build();
                Host.Start();

                if (!WaitForService(10000))
                {
                    Stop();
                    throw new InvalidOperationException($"Test service did not become ready at {BaseUri}");
                }

                Initialized = true;
            }
        }

        public static void Stop()
        {
            lock (SyncLock)
            {
                try
                {
                    Host?.StopAsync().GetAwaiter().GetResult();
                    Host?.Dispose();
                }
                finally
                {
                    Host = null;
                    Initialized = false;
                }
            }
        }

        private static bool WaitForService(int timeoutMs)
        {
            using var client = new HttpClient { BaseAddress = BaseUri };
            var started = DateTime.UtcNow;

            while ((DateTime.UtcNow - started).TotalMilliseconds < timeoutMs)
            {
                try
                {
                    var response = client.GetAsync("ipsafe/public").GetAwaiter().GetResult();
                    var statusCode = (int)response.StatusCode;
                    if (statusCode >= 200 && statusCode < 500)
                    {
                        return true;
                    }
                }
                catch
                {
                }

                Thread.Sleep(150);
            }

            return false;
        }

        private static int GetPortForCurrentProcess()
        {
            var pid = Process.GetCurrentProcess().Id;
            return 25000 + (pid % 10000);
        }
    }
}

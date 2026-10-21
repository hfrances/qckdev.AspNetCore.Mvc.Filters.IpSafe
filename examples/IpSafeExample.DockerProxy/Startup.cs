using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IpSafeExample.DockerProxy.Swagger;
using qckdev.AspNetCore.Mvc.Filters.IpSafe;
using qckdev.AspNetCore.ForwardedHeaders;

namespace IpSafeExample.DockerProxy
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddForwardedHeadersSettingsProvider<ForwardedHeadersSettingsProvider>(options =>
                    options.WithConfiguration(Configuration, "Security:ForwardedHeaders"))
                .AddIpSafeFilter<IpSafeSettingsProvider>(options =>
                    options.WithConfiguration(Configuration, "Security:IpSafeList"))
                .AddIpSafeFilter(IpSafeSchemes.Office, options =>
                    options.WithConfiguration(Configuration, $"Security:IpSafeSchemes:{IpSafeSchemes.Office}"))
                .AddIpSafeFilter(IpSafeSchemes.Partner, options =>
                    options.WithConfiguration(Configuration, $"Security:IpSafeSchemes:{IpSafeSchemes.Partner}"));
            services.AddControllers();
            services.AddSwagger();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders(options =>
                options.WithProvider<ForwardedHeadersSettingsProvider>());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();
            app.UseSwagger();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}

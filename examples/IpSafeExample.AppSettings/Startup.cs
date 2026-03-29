using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using qckdev.AspNetCore.Mvc.Filters.IpSafe;
using IpSafeExample.AppSettings.Swagger;

namespace IpSafeExample.AppSettings
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Modern setup: no legacy AddIpSafeFilter(settings) overload.
            services.AddIpSafeFilter<IpSafeSettingsProvider>();
            services.Configure<IpSafeListSettings>(Configuration.GetSection("IpSafeList"));
            services.AddControllers();
            services.AddSwagger();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseIpSafeFilter(cfg =>
            {
                cfg.WithConfiguration<IpSafeTrustedProxiesSettings>(Configuration, (config, target) =>
                    config.GetSection("IpSafeList:TrustedForwardedHeaders").Bind(target));
            });
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

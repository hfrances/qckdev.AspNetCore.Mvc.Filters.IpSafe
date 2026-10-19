using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using qckdev.AspNetCore.Mvc.Filters.IpSafe;
using IpSafeExample.Services.Swagger;

namespace IpSafeExample.Services
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
            services.AddOptions<IpSafeServiceSettings>()
                .Bind(Configuration.GetSection("Security:IpSafeList"))
                .Bind(Configuration.GetSection("Security:ForwardedHeaders"));
            services.AddScoped<IAppSecuritySettingsService, AppSecuritySettingsService>();
            services.AddScoped<ServiceTrustedProxiesProvider>();
            services.AddIpSafeFilter<ServiceIpSafeSettingsProvider>();
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

            app.UseIpSafeFilter(cfg =>
            {
                cfg.WithProxyService<ServiceTrustedProxiesProvider>();
            });
            app.UseHttpsRedirection();
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

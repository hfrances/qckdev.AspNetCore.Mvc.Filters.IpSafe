using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using qckdev.AspNetCore.ForwardedHeaders;
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
            services.AddForwardedHeadersSettingsProvider<ForwardedHeadersSettingsProvider>(options =>
                options.WithConfiguration(Configuration, "Security:ForwardedHeaders"));
            services.AddIpSafeFilter<IpSafeSettingsProvider>(options =>
                options.WithConfiguration(Configuration, "Security:IpSafeList"));
            services.AddControllers();
            services.AddSwagger();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders(options =>
                options.WithProvider<ForwardedHeadersSettingsProvider>());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

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

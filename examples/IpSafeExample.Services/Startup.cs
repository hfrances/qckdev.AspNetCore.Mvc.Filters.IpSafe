using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using qckdev.AspNetCore.Mvc.Filters.IpSafe;
using qckdev.AspNetCore.ForwardedHeaders;
using IpSafeExample.Services.Swagger;

namespace IpSafeExample.Services
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddForwardedHeadersSettingsProvider<AppSecuritySettingsService>();
            services.AddIpSafeFilter<AppSecuritySettingsService>();
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

            app.UseForwardedHeaders(options =>
                options.WithProvider<AppSecuritySettingsService>());
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

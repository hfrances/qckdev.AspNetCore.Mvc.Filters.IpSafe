using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using qckdev.AspNetCore.Mvc.Filters.IpSafe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IpSafeExample.DockerProxy.Controllers
{
    /// <summary>
    /// Demonstrates controller-level IpSafe filtering in a proxy-aware setup.
    /// </summary>
    [ApiController]
    [Route("[controller]"), IpSafeFilter]
    public class WeatherForecastGlobalController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastGlobalController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Returns weather forecasts for requests allowed by controller-level IpSafe rules.
        /// </summary>
        /// <response code="200">Forecast list returned successfully.</response>
        /// <response code="403">Request IP is not allowed by IpSafe.</response>
        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        /// <summary>
        /// Returns weather forecasts without IpSafe restrictions for this action.
        /// </summary>
        /// <response code="200">Forecast list returned successfully.</response>
        [HttpGet("public"), AllowAnyIpAddress]
        public IEnumerable<WeatherForecast> GetAnyIpAddress()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

    }
}

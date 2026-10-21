using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using qckdev.AspNetCore.Mvc.Filters.IpSafe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IpSafeExample.AppSettings.Controllers
{
    /// <summary>
    /// Provides weather data protected by endpoint-level IpSafe filtering from app settings.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Returns weather forecasts for requests allowed by IpSafe configuration.
        /// </summary>
        /// <response code="200">Forecast list returned successfully.</response>
        /// <response code="403">Request IP is not allowed by IpSafe.</response>
        [HttpGet, IpSafeFilter]
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

    }
}

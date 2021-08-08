using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCaching.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IDistributedCache _distributedCache;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IDistributedCache distributedCache)
        {
            _logger = logger;
            this._distributedCache = distributedCache;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> GetAsync()
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

        [HttpPut]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            var cacheKey = "weather";
            var dataFromCache = _distributedCache.Get(cacheKey);
            if (dataFromCache is null)
            {
                var rng = new Random();
                var weather = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = rng.Next(-20, 55),
                    Summary = Summaries[rng.Next(Summaries.Length)]
                })
            .ToArray();
                var options = new DistributedCacheEntryOptions().SetAbsoluteExpiration(DateTime.Now.AddMinutes(2)).
                    SetSlidingExpiration(TimeSpan.FromMinutes(1));
                var cacheData = JsonConvert.SerializeObject(weather);
                var encodedString = Encoding.UTF8.GetBytes(cacheData);
                _ = _distributedCache.SetAsync(cacheKey, encodedString, options);
                return weather;
            }
            else
            {
                var cachedOut = await _distributedCache.GetAsync(cacheKey);
                var cacheOutString = Encoding.UTF8.GetString(cachedOut);
                var cachedWeather = JsonConvert.DeserializeObject<IEnumerable<WeatherForecast>>(cacheOutString);
                return cachedWeather;
            }
        }
    }
}
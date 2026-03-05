using Microsoft.AspNetCore.Mvc;

namespace STBWeb.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExchangeRateController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetRates(string date, string lang = "mk")
        {
            // Mock data for exchange rates
            // In a real scenario, this would fetch from a database or external API based on 'date'
            var rates = new[]
            {
                new { Currency = "EUR", Country = lang == "en" ? "EMU" : "ЕМУ", Buy = 61.40, Middle = 61.50, Sell = 61.60 },
                new { Currency = "USD", Country = lang == "en" ? "USA" : "САД", Buy = 56.10, Middle = 56.50, Sell = 56.90 },
                new { Currency = "CHF", Country = lang == "en" ? "Switzerland" : "Швајцарија", Buy = 55.20, Middle = 55.50, Sell = 55.80 },
                new { Currency = "GBP", Country = lang == "en" ? "Great Britain" : "Велика Британија", Buy = 70.10, Middle = 70.50, Sell = 70.90 }
            };

            return Ok(rates);
        }
    }
}

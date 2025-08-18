using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using LibraryApi.OptionsConfiguration;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;

namespace LibraryApi.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [ApiVersion("1.0")]
    [EnableRateLimiting("general")]
    [ApiController, Route("api/v{version:apiVersion}/configurations")]
    public class ConfigurationsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly PersonOptions _personOptions;

        public ConfigurationsController(IConfiguration configuration, IOptions<PersonOptions> personOptions)
        {
            _configuration = configuration;
            _personOptions = personOptions.Value;
        }

        [HttpGet("person-options")]
        public ActionResult GetPersonOptions()
        {
            return Ok(_personOptions);
        }

        [HttpGet("appsettings")]
        public ActionResult GetDirectlyFromAppSettings()
        {
            var name = _configuration.GetValue<string>("PersonOptions:Name");
            var age = _configuration.GetValue<string>("PersonOptions:Age");
            return Ok(name + " is " + age + " years old.");
        }
    }
}
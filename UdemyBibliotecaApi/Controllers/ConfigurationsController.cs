namespace UdemyBibliotecaApi.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using System.Threading.Tasks;

    [ApiController]
    [Route("api/configurations")]
    public class ConfigurationsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ConfigurationsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public ActionResult<string> Get()
        {
            var lastName1 = _configuration["lastName"];

            //var lastName2 = _configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
            var lastName2 = _configuration.GetValue<string>("lastName");
            return lastName2!;
        }
    }
}
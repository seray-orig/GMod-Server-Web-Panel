using Microsoft.AspNetCore.Mvc;

namespace GMServerWebPanel.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TitlesController(IConfiguration configuration) : ControllerBase
    {
        private readonly IConfiguration _configuration = configuration;

        [HttpGet("login-page")]
        public IActionResult GetLoginPageTitle()
        {
            var titleH1 = _configuration["ProgramTitles:LoginH1"];
            var titleH3 = _configuration["ProgramTitles:LoginH3"];

            return Ok(new { titleH1, titleH3 });
        }
    }
}

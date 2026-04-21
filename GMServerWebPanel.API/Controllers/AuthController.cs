using GMServerWebPanel.API.Models;
using Microsoft.AspNetCore.Mvc;
using GMServerWebPanel.API.Services.Interfaces;

namespace GMServerWebPanel.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IUserService userService, ITokenServise jwtService) : ControllerBase
    {
        private readonly IUserService _userService = userService;
        private readonly ITokenServise _jwtService = jwtService;

        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
        {
            var user = _userService.GetUserBy(request.Login, request.Password);

            if (user == null)
                return Unauthorized();

            var token = _jwtService.GenerateToken(user, request.RememberMe);

            return Ok(new { token });
        }
    }
}

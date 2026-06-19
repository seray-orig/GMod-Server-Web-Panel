using GMServerWebPanel.API.Models;
using GMServerWebPanel.API.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GMServerWebPanel.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ServerController(IServerProcessController agentClient) : ControllerBase
    {
        private readonly IServerProcessController _agentClient = agentClient;

        [HttpPost("command")]
        public async Task<IActionResult> SendCommand(SendCommand command)
        {
            await _agentClient.SendCommandAsync(command.Command);
            return Ok();
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start()
        {
            await _agentClient.StartServerAsync();
            return Ok();
        }

        [HttpPost("stop")]
        public async Task<IActionResult> Stop()
        {
            await _agentClient.StopServerAsync();
            return Ok();
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update()
        {
            await _agentClient.UpdateServerAsync();
            return Ok();
        }
    }
}

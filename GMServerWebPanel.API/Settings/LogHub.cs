using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GMServerWebPanel.API.Settings
{
    [Authorize]
    public class LogHub : Hub { }
}

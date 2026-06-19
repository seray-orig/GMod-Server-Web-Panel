using Microsoft.AspNetCore.SignalR;
using GMServerWebPanel.API.Shared;
using GMServerWebPanel.API.Settings;

namespace GMServerWebPanel.API.Services
{
    public class LogStreamerService : BackgroundService
    {
        private readonly IServerProcessController _agentClient;
        private readonly IHubContext<LogHub> _hubContext;

        public LogStreamerService(IServerProcessController agentClient, IHubContext<LogHub> hubContext)
        {
            _agentClient = agentClient;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await foreach (var logLine in _agentClient.StreamLogsAsync().WithCancellation(stoppingToken))
                    {
                        await _hubContext.Clients.All.SendAsync("ReceiveLog", logLine, cancellationToken: stoppingToken);
                    }
                }
                catch
                {
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
    }
}

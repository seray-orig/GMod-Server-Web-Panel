using Microsoft.AspNetCore.SignalR;
using GMServerWebPanel.API.Shared;
using GMServerWebPanel.API.Settings;

namespace GMServerWebPanel.API.Services
{
    public class LogStreamerService : BackgroundService
    {
        private readonly IServerProcessController _agentClient;
        private readonly IHubContext<LogHub> _hubContext;
        private readonly ILogger<LogStreamerService> _logger;

        public LogStreamerService(IServerProcessController agentClient, IHubContext<LogHub> hubContext, ILogger<LogStreamerService> logger)
        {
            _agentClient = agentClient;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LogStreamerService запущен.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await foreach (var logLine in _agentClient.StreamLogsAsync().WithCancellation(stoppingToken))
                    {
                        await _hubContext.Clients.All.SendAsync("ReceiveLog", logLine, cancellationToken: stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при чтении стрима логов. Повтор через 5 сек.");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }

        public LogStreamerService(IServerProcessController agentClient, IHubContext<LogHub> hubContext)
        {
            _agentClient = agentClient;
            _hubContext = hubContext;
        }
    }
}

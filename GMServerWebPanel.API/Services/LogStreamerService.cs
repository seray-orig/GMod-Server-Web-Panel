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
            Console.WriteLine("[LogStreamer] Сервис запущен!");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("[LogStreamer] Подключение к агенту...");
                    await foreach (var logLine in _agentClient.StreamLogsAsync().WithCancellation(stoppingToken))
                    {
                        Console.WriteLine($"[LogStreamer] Получена строка: {logLine}");
                        await _hubContext.Clients.All.SendAsync("ReceiveLog", logLine, cancellationToken: stoppingToken);
                        Console.WriteLine($"[LogStreamer] Отправлено в SignalR: {logLine}");
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("[LogStreamer] Операция отменена (переподключение)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LogStreamer] Ошибка: {ex.Message}");
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    Console.WriteLine("[LogStreamer] Переподключение через 5 секунд...");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
    }
}

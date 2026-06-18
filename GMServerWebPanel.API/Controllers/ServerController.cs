using GMServerWebPanel.API.Models;
using GMServerWebPanel.API.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grpc.Net.Client;
using ProtoBuf.Grpc.Client;
using System;
using System.Threading.Tasks;

namespace GMServerWebPanel.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ServerController : ControllerBase
    {
        private readonly string _agentUrl;

        public ServerController(IConfiguration config)
        {
            // Берем URL Агента из appsettings.json, либо используем дефолтный localhost
            _agentUrl = config["ServerUrls"] ?? "http://localhost:50051";
        }

        // Вспомогательный приватный метод, который динамически создает подключение к Агенту на каждый запрос
        private IServerProcessController GetAgentClient()
        {
            // Включаем поддержку HTTP/2 без шифрования (без HTTPS) на лету
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            // Создаем канал связи
            var channel = GrpcChannel.ForAddress(_agentUrl, new GrpcChannelOptions
            {
                HttpHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                }
            });

            // Магически генерируем живой прокси-клиент из общего интерфейса
            return channel.CreateGrpcService<IServerProcessController>();
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start()
        {
            try
            {
                var agent = GetAgentClient(); // Динамическое переподключение
                bool result = await agent.StartServerAsync();
                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Бэкенд Ошибка Кнопки Старт]: Не удалось связаться с Агентом. {ex.Message}");
                return StatusCode(503, "Агент управления процессами недоступен. Попробуйте позже.");
            }
        }

        [HttpPost("stop")]
        public async Task<IActionResult> Stop()
        {
            try
            {
                var agent = GetAgentClient(); // Динамическое переподключение
                bool result = await agent.StopServerAsync();
                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Бэкенд Ошибка Кнопки Стоп]: Не удалось связаться с Агентом. {ex.Message}");
                return StatusCode(503, "Агент управления процессами недоступен. Попробуйте позже.");
            }
        }

        [HttpPost("command")]
        public async Task<IActionResult> SendCommand(SendCommand command)
        {
            try
            {
                var agent = GetAgentClient(); // Динамическое переподключение
                await agent.SendCommandAsync(command.Command);
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Бэкенд Ошибка Команды]: Не удалось связаться с Агентом. {ex.Message}");
                return StatusCode(503, "Агент управления процессами недоступен.");
            }
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs()
        {
            try
            {
                var agent = GetAgentClient(); // Динамическое переподключение каждые 2 секунды от шорт-пуллинга
                var logsList = await agent.GetLatestLogsAsync();
                return Ok(new { logs = logsList });
            }
            catch (Exception ex)
            {
                // Если агент выключен, шорт-пуллинг не уронит бэкенд, а просто выведет ошибку в консоль
                Console.WriteLine($"[Бэкенд Ошибка Логов]: Агент спит или перезагружается... {ex.Message}");

                // Возвращаем пустой массив логов фронтенду, пока связи нет, чтобы React не падал
                return Ok(new { logs = new[] { "[Панель]: Ожидание связи с Агентом управления..." } });
            }
        }
    }
}

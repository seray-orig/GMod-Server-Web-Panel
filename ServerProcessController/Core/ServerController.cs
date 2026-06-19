using GMServerWebPanel.API.Shared;
using System.Diagnostics;
using System.Threading.Channels;

namespace GMServerWebPanel.API.ServerProcessController.Core
{
    internal sealed class ServerController : IServerProcessController
    {
        private readonly Channel<string> _logChannel = Channel.CreateUnbounded<string>();
        private Process? _serverProcess;

        private ProcessStartInfo GetStartInfo() => new()
        {
            FileName = "./mainServer/srcds_run",
            Arguments = "-console -game garrysmod +gamemode terrortown +host_workshop_collection 2792454072 +map ttt_rooftops_2016_v1 +maxplayers 50 -port 27015 -steamport 26900 +clientport 27005",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Локальный синхронный запуск (для тестов внутри Агента)
        public bool Start()
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                _logChannel.Writer.TryWrite("[Контроллер]: Сервер уже запущен.");
                return false;
            }

            var startInfo = GetStartInfo();
            if (!File.Exists(startInfo.FileName))
            {
                _logChannel.Writer.TryWrite($"[Ошибка ОС]: Файл не найден по пути {startInfo.FileName}");
                return false;
            }

            _serverProcess = new Process { StartInfo = startInfo };

            // Мгновенно перехватываем каждую строчку вывода консоли GMod по сети
            _serverProcess.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    _logChannel.Writer.TryWrite(args.Data);
                }
            };

            // Перехватываем системные ошибки сегментации движка Source (Segmentation fault)
            _serverProcess.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    _logChannel.Writer.TryWrite($"[ОШИБКА LINUX SRCDS]: {args.Data}");
                }
            };

            try
            {
                _serverProcess.Start();

                // Начинаем асинхронное чтение потоков
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();

                _logChannel.Writer.TryWrite("[Контроллер]: Процесс srcds_run успешно запущен. Потоки ввода-вывода перенаправлены.");

                // Фоновое отслеживание смерти процесса, чтобы сообщить в логи панели
                _ = _serverProcess.WaitForExitAsync().ContinueWith(_ =>
                {
                    _logChannel.Writer.TryWrite("[Контроллер]: Игровой сервер завершил свою работу.");
                });

                return true;
            }
            catch (Exception ex)
            {
                _logChannel.Writer.TryWrite($"[Ошибка запуска]: {ex.Message}");
                return false;
            }
        }

        // Реализация метода интерфейса gRPC (для вызова из Веб-Панели)
        public Task StartServerAsync()
        {
            Start();
            return Task.CompletedTask;
        }

        // Остановка сервера
        public Task StopServerAsync()
        {
            if (_serverProcess == null || _serverProcess.HasExited)
            {
                _logChannel.Writer.TryWrite("[Контроллер]: Сервер уже остановлен.");
                return Task.CompletedTask;
            }

            _logChannel.Writer.TryWrite("[Контроллер]: Отправка команды завершения (exit) в консоль сервера...");

            try
            {
                // Мягко просим Source-сервер закрыться
                _serverProcess.StandardInput.WriteLine("exit");
            }
            catch (Exception ex)
            {
                _logChannel.Writer.TryWrite($"[Ошибка остановки]: {ex.Message}");
            }

            _logChannel.Writer.TryWrite("[Контроллер]: Игровой сервер остановлен.");
            return Task.CompletedTask;
        }

        // Отправка команды в консоль GMod
        public Task SendCommandAsync(string command)
        {
            if (_serverProcess == null || _serverProcess.HasExited)
            {
                _logChannel.Writer.TryWrite("[Контроллер]: Невозможно отправить команду. Сервер выключен.");
                return Task.CompletedTask;
            }

            try
            {
                _serverProcess.StandardInput.WriteLine(command);
                _logChannel.Writer.TryWrite($"] {command}"); // Дублируем отправленную команду в логи для истории
            }
            catch (Exception ex)
            {
                _logChannel.Writer.TryWrite($"[Ошибка отправки команды]: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        // Поток логов (gRPC стрим)
        public async IAsyncEnumerable<string> StreamLogsAsync()
        {
            while (await _logChannel.Reader.WaitToReadAsync())
            {
                while (_logChannel.Reader.TryRead(out var logLine))
                {
                    yield return logLine;
                }
            }
        }

        // Заглушка для обновления через SteamCMD
        public Task UpdateServerAsync()
        {
            _logChannel.Writer.TryWrite("[Контроллер]: Запущено обновление сервера (Заглушка)...");
            // Сюда позже вставите логику вызова SteamCMD, которую мы обсуждали ранее
            return Task.CompletedTask;
        }
    }
}

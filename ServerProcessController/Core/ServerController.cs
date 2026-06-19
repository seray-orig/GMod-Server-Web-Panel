using GMServerWebPanel.API.Shared;
using System.Diagnostics;
using System.Threading.Channels;

namespace GMServerWebPanel.API.ServerProcessController.Core
{
    internal sealed class ServerController : IServerProcessController
    {
        private readonly Channel<string> _logChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        private Process? _serverProcess;
        private readonly object _processLock = new();

        private ProcessStartInfo GetStartInfo() => new()
        {
            FileName = "./mainServer/srcds_run",
            Arguments = "-console -game garrysmod +gamemode terrortown +host_workshop_collection 2792454072 +map ttt_rooftops_2016_v1 +maxplayers 50 -port 27015 -steamport 26900 +clientport 27005",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName("./mainServer/srcds_run") ?? "."
        };

        public Task StartServerAsync()
        {
            lock (_processLock)
            {
                if (_serverProcess != null && !_serverProcess.HasExited)
                {
                    _logChannel.Writer.TryWrite("[Контроллер]: Сервер уже запущен.");
                    return Task.CompletedTask;
                }

                var startInfo = GetStartInfo();
                if (!File.Exists(startInfo.FileName))
                {
                    _logChannel.Writer.TryWrite($"[Ошибка ОС]: Файл не найден по пути {startInfo.FileName}");
                    return Task.CompletedTask;
                }

                _serverProcess = new Process { StartInfo = startInfo };

                _serverProcess.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                        _ = _logChannel.Writer.WriteAsync(args.Data);
                };

                _serverProcess.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                        _ = _logChannel.Writer.WriteAsync($"[ОШИБКА LINUX SRCDS]: {args.Data}");
                };

                try
                {
                    _serverProcess.Start();
                    _serverProcess.BeginOutputReadLine();
                    _serverProcess.BeginErrorReadLine();

                    _ = _serverProcess.WaitForExitAsync().ContinueWith(_ =>
                    {
                        _ = _logChannel.Writer.WriteAsync("[Контроллер]: Игровой сервер завершил свою работу.");
                        _serverProcess = null; // Сброс ссылки
                    });

                    _ = _logChannel.Writer.WriteAsync("[Контроллер]: Процесс srcds_run успешно запущен.");
                }
                catch (Exception ex)
                {
                    _ = _logChannel.Writer.WriteAsync($"[Ошибка запуска]: {ex.Message}");
                    _serverProcess = null;
                }
            }

            return Task.CompletedTask;
        }

        public async Task StopServerAsync()
        {
            Process? process;
            lock (_processLock)
            {
                process = _serverProcess;
                if (process == null || process.HasExited)
                {
                    await _logChannel.Writer.WriteAsync("[Контроллер]: Сервер уже остановлен.");
                    return;
                }
            }

            await _logChannel.Writer.WriteAsync("[Контроллер]: Отправка команды 'exit'...");

            try
            {
                process.StandardInput.WriteLine("exit");
                process.StandardInput.Close(); // Важно!
            }
            catch (Exception ex)
            {
                await _logChannel.Writer.WriteAsync($"[Ошибка остановки]: {ex.Message}");
            }

            // Даем 10 секунд на graceful shutdown
            bool exited = await Task.Run(() => process.WaitForExit(10_000));
            if (!exited)
            {
                try
                {
                    process.Kill();
                    await _logChannel.Writer.WriteAsync("[Контроллер]: Сервер принудительно завершён.");
                }
                catch { /* ignore */ }
            }

            lock (_processLock)
            {
                _serverProcess = null;
            }

            await _logChannel.Writer.WriteAsync("[Контроллер]: Игровой сервер остановлен.");
        }

        public async Task SendCommandAsync(string command)
        {
            Process? process;
            lock (_processLock)
            {
                process = _serverProcess;
                if (process == null || process.HasExited)
                {
                    await _logChannel.Writer.WriteAsync("[Контроллер]: Невозможно отправить команду. Сервер выключен.");
                    return;
                }
            }

            try
            {
                process.StandardInput.WriteLine(command);
                await _logChannel.Writer.WriteAsync($"] {command}");
            }
            catch (Exception ex)
            {
                await _logChannel.Writer.WriteAsync($"[Ошибка отправки команды]: {ex.Message}");
            }
        }

        public async IAsyncEnumerable<string> StreamLogsAsync()
        {
            var reader = _logChannel.Reader;
            while (await reader.WaitToReadAsync())
            {
                // Читаем ВСЕ доступные сообщения за один проход
                while (reader.TryRead(out var line))
                {
                    yield return line;
                }
            }
            // Канал никогда не закрывается, поэтому этот цикл бесконечен.
            // Но это нормально для long-lived gRPC stream.
        }

        public async Task UpdateServerAsync()
        {
            await _logChannel.Writer.WriteAsync("[Контроллер]: Запущено обновление сервера (заглушка).");
            // TODO: Реализовать SteamCMD
        }
    }
}

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
        private readonly SemaphoreSlim _processLock = new(1, 1); // Async-safe блокировка

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

        public async Task StartServerAsync()
        {
            await _processLock.WaitAsync();
            try
            {
                if (_serverProcess != null && !_serverProcess.HasExited)
                {
                    await _logChannel.Writer.WriteAsync("[Контроллер]: Сервер уже запущен.");
                    return;
                }

                var startInfo = GetStartInfo();
                if (!File.Exists(startInfo.FileName))
                {
                    await _logChannel.Writer.WriteAsync($"[Ошибка ОС]: Файл не найден по пути {startInfo.FileName}");
                    return;
                }

                _serverProcess = new Process { StartInfo = startInfo };

                _serverProcess.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                        _ = _logChannel.Writer.WriteAsync(args.Data).AsTask();
                };

                _serverProcess.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                        _ = _logChannel.Writer.WriteAsync($"[ОШИБКА LINUX SRCDS]: {args.Data}").AsTask();
                };

                try
                {
                    _serverProcess.Start();
                    _serverProcess.BeginOutputReadLine();
                    _serverProcess.BeginErrorReadLine();

                    _ = _serverProcess.WaitForExitAsync().ContinueWith(async _ =>
                    {
                        await _logChannel.Writer.WriteAsync("[Контроллер]: Игровой сервер завершил свою работу.");
                        await _processLock.WaitAsync();
                        try
                        {
                            _serverProcess = null;
                        }
                        finally
                        {
                            _processLock.Release();
                        }
                    });

                    await _logChannel.Writer.WriteAsync("[Контроллер]: Процесс srcds_run успешно запущен.");
                }
                catch (Exception ex)
                {
                    await _logChannel.Writer.WriteAsync($"[Ошибка запуска]: {ex.Message}");
                    _serverProcess = null;
                }
            }
            finally
            {
                _processLock.Release();
            }
        }

        public async Task StopServerAsync()
        {
            await _processLock.WaitAsync();
            Process? process;
            try
            {
                process = _serverProcess;
                if (process == null || process.HasExited)
                {
                    await _logChannel.Writer.WriteAsync("[Контроллер]: Сервер уже остановлен.");
                    return;
                }
            }
            finally
            {
                _processLock.Release();
            }

            await _logChannel.Writer.WriteAsync("[Контроллер]: Отправка команды 'exit'...");

            try
            {
                process.StandardInput.WriteLine("exit");
                process.StandardInput.Close();
            }
            catch (Exception ex)
            {
                await _logChannel.Writer.WriteAsync($"[Ошибка остановки]: {ex.Message}");
            }

            bool exited = await Task.Run(() => process.WaitForExit(10_000));
            if (!exited)
            {
                try
                {
                    process.Kill();
                    await _logChannel.Writer.WriteAsync("[Контроллер]: Сервер принудительно завершён.");
                }
                catch { }
            }

            await _processLock.WaitAsync();
            try
            {
                _serverProcess = null;
            }
            finally
            {
                _processLock.Release();
            }

            await _logChannel.Writer.WriteAsync("[Контроллер]: Игровой сервер остановлен.");
        }

        public async Task SendCommandAsync(string command)
        {
            await _processLock.WaitAsync();
            Process? process;
            try
            {
                process = _serverProcess;
                if (process == null || process.HasExited)
                {
                    await _logChannel.Writer.WriteAsync("[Контроллер]: Невозможно отправить команду. Сервер выключен.");
                    return;
                }
            }
            finally
            {
                _processLock.Release();
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
                while (reader.TryRead(out var line))
                {
                    yield return line;
                }
            }
        }

        public async Task UpdateServerAsync()
        {
            await _logChannel.Writer.WriteAsync("[Контроллер]: Запущено обновление сервера (заглушка).");
        }
    }
}

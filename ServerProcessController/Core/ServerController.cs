using GMServerWebPanel.API.Shared;
using Porta.Pty;
using System.Text;
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

        private IPtyConnection? _terminal;
        private readonly SemaphoreSlim _processLock = new(1, 1);
        private CancellationTokenSource? _readCts;

        public async Task StartServerAsync()
        {
            await _processLock.WaitAsync();
            try
            {
                if (_terminal != null)
                {
                    await _logChannel.Writer.WriteAsync("[Контроллер]: Сервер уже запущен.");
                    return;
                }

                var serverPath = Path.GetFullPath("./mainServer/srcds_run");
                if (!File.Exists(serverPath))
                {
                    await _logChannel.Writer.WriteAsync($"[Ошибка ОС]: Файл не найден по пути {serverPath}");
                    return;
                }

                var options = new PtyOptions
                {
                    Name = "GMod Server",
                    Cols = 120,
                    Rows = 30,
                    Cwd = Path.GetDirectoryName(serverPath) ?? ".",
                    App = serverPath,
                    CommandLine = new[]
                    {
                        "-console",
                        "-game", "garrysmod",
                        "+gamemode", "terrortown",
                        "+host_workshop_collection", "2792454072",
                        "+map", "ttt_rooftops_2016_v1",
                        "+maxplayers", "50",
                        "-port", "27015",
                        "-steamport", "26900",
                        "+clientport", "27005"
                    }
                };

                try
                {
                    _terminal = await PtyProvider.SpawnAsync(options, CancellationToken.None);

                    _readCts = new CancellationTokenSource();
                    _ = Task.Run(() => ReadOutputAsync(_terminal.ReaderStream, _readCts.Token));

                    _terminal.ProcessExited += (sender, e) =>
                    {
                        _ = _logChannel.Writer.WriteAsync("[Контроллер]: Игровой сервер завершил свою работу.");
                        _processLock.Wait();
                        try
                        {
                            _terminal = null;
                            _readCts?.Cancel();
                        }
                        finally
                        {
                            _processLock.Release();
                        }
                    };

                    await _logChannel.Writer.WriteAsync("[Контроллер]: Процесс srcds_run успешно запущен через PTY.");
                }
                catch (Exception ex)
                {
                    await _logChannel.Writer.WriteAsync($"[Ошибка запуска]: {ex.Message}");
                    _terminal = null;
                }
            }
            finally
            {
                _processLock.Release();
            }
        }

        private async Task ReadOutputAsync(Stream reader, CancellationToken ct)
        {
            var buffer = new byte[4096];
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    int bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length, ct);
                    if (bytesRead == 0) break;

                    string output = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // Разбиваем на строки и отправляем в канал
                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        await _logChannel.Writer.WriteAsync(line);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Нормальное завершение
            }
            catch (Exception ex)
            {
                await _logChannel.Writer.WriteAsync($"[Ошибка чтения вывода]: {ex.Message}");
            }
        }

        public async Task StopServerAsync()
        {
            await _processLock.WaitAsync();
            IPtyConnection? terminal;
            try
            {
                terminal = _terminal;
                if (terminal == null)
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
                byte[] exitCommand = Encoding.UTF8.GetBytes("exit\r\n");
                await terminal.WriterStream.WriteAsync(exitCommand, 0, exitCommand.Length);
                await terminal.WriterStream.FlushAsync();
            }
            catch (Exception ex)
            {
                await _logChannel.Writer.WriteAsync($"[Ошибка остановки]: {ex.Message}");
            }

            // Ждём до 10 секунд
            bool exited = await Task.Run(() => terminal.WaitForExit(10_000));
            if (!exited)
            {
                try
                {
                    terminal.Kill();
                    await _logChannel.Writer.WriteAsync("[Контроллер]: Сервер принудительно завершён.");
                }
                catch { }
            }

            _readCts?.Cancel();

            await _processLock.WaitAsync();
            try
            {
                _terminal = null;
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
            IPtyConnection? terminal;
            try
            {
                terminal = _terminal;
                if (terminal == null)
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
                byte[] cmdBytes = Encoding.UTF8.GetBytes(command + "\r\n");
                await terminal.WriterStream.WriteAsync(cmdBytes, 0, cmdBytes.Length);
                await terminal.WriterStream.FlushAsync();
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

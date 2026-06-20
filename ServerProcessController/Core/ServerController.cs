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

        // Использую пакет Porta.Pty для создания иллюзии терминала.
        // Обычный перехват ввода / вывода у Process не поддерживается у сервера gmod.
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

                var serverPath = "/home/garrysmod/mainServer/srcds_run";

                if (!File.Exists(serverPath))
                {
                    await _logChannel.Writer.WriteAsync($"[Ошибка]: Файл не найден: {serverPath}");
                    return;
                }

                var argsArray = new[]
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
                };

                var options = new PtyOptions
                {
                    Name = "GMod Server",
                    Cols = 120,
                    Rows = 30,
                    Cwd = "/home/garrysmod/mainServer",
                    App = serverPath,
                    CommandLine = argsArray
                };

                try
                {
                    _terminal = await PtyProvider.SpawnAsync(options, CancellationToken.None);
                    _readCts = new CancellationTokenSource();
                    _ = Task.Run(() => ReadOutputAsync(_terminal.ReaderStream, _readCts.Token));

                    _terminal.ProcessExited += (sender, e) =>
                    {
                        _ = _logChannel.Writer.WriteAsync("[Контроллер]: Игровой сервер завершил работу.");
                        _processLock.Wait();
                        try { _terminal = null; _readCts?.Cancel(); }
                        finally { _processLock.Release(); }
                    };

                    await _logChannel.Writer.WriteAsync("[Контроллер]: Сервер успешно запущен.");
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
            finally { _processLock.Release(); }

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

            bool exited = await Task.Run(() => terminal.WaitForExit(10_000));
            if (!exited)
            {
                try { terminal.Kill(); await _logChannel.Writer.WriteAsync("[Контроллер]: Сервер принудительно завершён."); }
                catch { }
            }

            _readCts?.Cancel();
            await _processLock.WaitAsync();
            try { _terminal = null; }
            finally { _processLock.Release(); }

            await _logChannel.Writer.WriteAsync("[Контроллер]: Игровой сервер остановлен.");
        }

        public async Task UpdateServerAsync()
        {
            // Удалено до лучших времён.
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
                    await _logChannel.Writer.WriteAsync("[Контроллер]: Сервер выключен.");
                    return;
                }
            }
            finally { _processLock.Release(); }

            try
            {
                byte[] cmdBytes = Encoding.UTF8.GetBytes(command + "\r\n");
                await terminal.WriterStream.WriteAsync(cmdBytes, 0, cmdBytes.Length);
                await terminal.WriterStream.FlushAsync();
                await _logChannel.Writer.WriteAsync($"] {command}");
            }
            catch (Exception ex)
            {
                await _logChannel.Writer.WriteAsync($"[Ошибка команды]: {ex.Message}");
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

        // Без этого Porta.Pty выдаёт дё рган  ный текст.
        private async Task ReadOutputAsync(Stream reader, CancellationToken ct)
        {
            var buffer = new byte[4096];
            var lineBuffer = new StringBuilder();

            while (!ct.IsCancellationRequested)
            {
                int bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length, ct);
                if (bytesRead == 0) break;

                string output = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                lineBuffer.Append(output);

                string content = lineBuffer.ToString();
                int lastNewline = content.LastIndexOfAny(new[] { '\r', '\n' });

                if (lastNewline >= 0)
                {
                    string fullLines = content.Substring(0, lastNewline);
                    var lines = fullLines.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            await _logChannel.Writer.WriteAsync(line);
                        }
                    }

                    lineBuffer.Clear();
                    lineBuffer.Append(content.Substring(lastNewline + 1));
                }
            }

            if (lineBuffer.Length > 0)
            {
                string remaining = lineBuffer.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(remaining))
                {
                    await _logChannel.Writer.WriteAsync(remaining);
                }
            }
        }
    }
}

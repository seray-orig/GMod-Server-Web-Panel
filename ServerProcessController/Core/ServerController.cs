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

        // Путь к SteamCMD
        private const string SteamCmdPath = "/home/garrysmod/.local/share/Steam/steamcmd/steamcmd.sh";
        // Путь к серверу
        private const string ServerInstallDir = "/home/garrysmod/mainServer";
        // App ID GMod сервера
        private const uint AppId = 4020;

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

                var serverPath = Path.Combine(ServerInstallDir, "srcds_run");

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
                    Cwd = ServerInstallDir,
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
            await _processLock.WaitAsync();
            try
            {
                // 1. Останавливаем сервер если запущен
                if (_terminal != null)
                {
                    await _logChannel.Writer.WriteAsync("[Обновление]: Остановка сервера перед обновлением...");

                    try
                    {
                        byte[] exitCommand = Encoding.UTF8.GetBytes("exit\r\n");
                        await _terminal.WriterStream.WriteAsync(exitCommand, 0, exitCommand.Length);
                        await _terminal.WriterStream.FlushAsync();

                        bool exited = await Task.Run(() => _terminal.WaitForExit(15_000));
                        if (!exited)
                        {
                            _terminal.Kill();
                            await _logChannel.Writer.WriteAsync("[Обновление]: Сервер принудительно завершён.");
                        }
                    }
                    catch (Exception ex)
                    {
                        await _logChannel.Writer.WriteAsync($"[Обновление] Ошибка остановки: {ex.Message}");
                    }

                    _readCts?.Cancel();
                    _terminal = null;
                    await Task.Delay(2000);
                }

                // 2. Проверяем SteamCMD
                if (!File.Exists(SteamCmdPath))
                {
                    await _logChannel.Writer.WriteAsync($"[Обновление] Ошибка: SteamCMD не найден по пути {SteamCmdPath}");
                    return;
                }

                await _logChannel.Writer.WriteAsync("[Обновление]: Запуск SteamCMD...");

                var steamCmdArgs = new[]
                {
                    "+force_install_dir", ServerInstallDir,
                    "+login", "anonymous",
                    "+app_update", AppId.ToString(), "validate",
                    "+quit"
                };

                IPtyConnection? steamCmdTerminal = null;
                var updateCompletedTcs = new TaskCompletionSource<bool>();
                var readCts = new CancellationTokenSource();

                try
                {
                    var options = new PtyOptions
                    {
                        Name = "SteamCMD",
                        Cols = 120,
                        Rows = 30,
                        Cwd = Path.GetDirectoryName(SteamCmdPath) ?? ".",
                        App = SteamCmdPath,
                        CommandLine = steamCmdArgs
                    };

                    steamCmdTerminal = await PtyProvider.SpawnAsync(options, CancellationToken.None);

                    // Читаем вывод и ищем маркеры завершения
                    _ = Task.Run(async () =>
                    {
                        await ReadSteamCmdOutputAsync(steamCmdTerminal.ReaderStream, readCts.Token, updateCompletedTcs);
                    });

                    await _logChannel.Writer.WriteAsync("[Обновление]: Ожидание завершения обновления...");

                    // Ждём либо завершения обновления (по маркеру), либо таймаута 30 минут
                    var timeoutTask = Task.Delay(TimeSpan.FromMinutes(30));
                    var completedTask = await Task.WhenAny(updateCompletedTcs.Task, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        steamCmdTerminal.Kill();
                        readCts.Cancel();
                        await _logChannel.Writer.WriteAsync("[Обновление] Ошибка: Таймаут 30 минут. SteamCMD не завершился.");
                        return;
                    }

                    // Проверяем результат
                    bool success = await updateCompletedTcs.Task;

                    if (success)
                    {
                        await _logChannel.Writer.WriteAsync("[Обновление]: Обновление успешно завершено!");
                    }
                    else
                    {
                        await _logChannel.Writer.WriteAsync("[Обновление]: Обновление завершено с ошибкой!");
                        return;
                    }

                    // Ждём завершения процесса SteamCMD (он должен закрыться сам после +quit)
                    bool processExited = await Task.Run(() => steamCmdTerminal.WaitForExit(10_000));
                    if (!processExited)
                    {
                        steamCmdTerminal.Kill();
                    }

                    readCts.Cancel();

                    // 3. Запускаем сервер снова
                    await _logChannel.Writer.WriteAsync("[Обновление]: Запуск сервера...");

                    _processLock.Release();
                    await StartServerAsync();
                    await _processLock.WaitAsync();

                    await _logChannel.Writer.WriteAsync("[Обновление]: Сервер перезапущен после обновления.");
                }
                catch (Exception ex)
                {
                    await _logChannel.Writer.WriteAsync($"[Обновление] Ошибка: {ex.Message}");
                }
                finally
                {
                    steamCmdTerminal?.Dispose();
                    readCts?.Cancel();
                }
            }
            finally
            {
                _processLock.Release();
            }
        }

        // Специальный метод для парсинга вывода SteamCMD
        private async Task ReadSteamCmdOutputAsync(Stream reader, CancellationToken ct, TaskCompletionSource<bool> completionSource)
        {
            var buffer = new byte[4096];
            var lineBuffer = new StringBuilder();

            try
            {
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

                                // Ищем маркеры завершения
                                if (line.Contains("Success! App", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!completionSource.Task.IsCompleted)
                                    {
                                        completionSource.SetResult(true);
                                    }
                                }
                                else if (line.Contains("ERROR!", StringComparison.OrdinalIgnoreCase) ||
                                         line.Contains("Error!", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!completionSource.Task.IsCompleted)
                                    {
                                        completionSource.SetResult(false);
                                    }
                                }
                            }
                        }

                        lineBuffer.Clear();
                        lineBuffer.Append(content.Substring(lastNewline + 1));
                    }
                }

                // Если поток завершился без маркеров
                if (!completionSource.Task.IsCompleted)
                {
                    completionSource.SetResult(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                await _logChannel.Writer.WriteAsync($"[Ошибка чтения SteamCMD]: {ex.Message}");
                if (!completionSource.Task.IsCompleted)
                {
                    completionSource.SetResult(false);
                }
            }
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

        private async Task ReadOutputAsync(Stream reader, CancellationToken ct)
        {
            var buffer = new byte[4096];
            var lineBuffer = new StringBuilder();

            try
            {
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
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                await _logChannel.Writer.WriteAsync($"[Ошибка чтения]: {ex.Message}");
            }
        }
    }
}

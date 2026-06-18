using GMServerWebPanel.API.Shared;
using System.Diagnostics;

namespace GMServerWebPanel.API.ServerProcessController.Core
{
    internal sealed class ServerController : IServerProcessController
    {
        // Обычный буфер для хранения последних 200 строк лога
        private readonly List<string> _logBuffer = new();
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

        public Task<bool> StartServerAsync()
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                lock (_logBuffer) { _logBuffer.Add("[Контроллер]: Сервер уже работает."); }
                return Task.FromResult(false);
            }

            try
            {
                var startInfo = GetStartInfo();
                _serverProcess = new Process { StartInfo = startInfo };

                // Ловим вывод и просто пишем в массив в памяти
                _serverProcess.OutputDataReceived += (s, args) =>
                {
                    if (args.Data == null) return;
                    lock (_logBuffer)
                    {
                        _logBuffer.Add(args.Data);
                        if (_logBuffer.Count > 200) _logBuffer.RemoveAt(0); // Храним только последние 200 строк
                    }
                };

                _serverProcess.Start();
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();

                lock (_logBuffer) { _logBuffer.Add("[Контроллер]: Процесс успешно запущен ОС!"); }
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                lock (_logBuffer) { _logBuffer.Add($"[Ошибка запуска]: {ex.Message}"); }
                return Task.FromResult(false);
            }
        }

        public Task<bool> StopServerAsync()
        {
            if (_serverProcess == null || _serverProcess.HasExited)
            {
                lock (_logBuffer) { _logBuffer.Add("[Контроллер]: Некого выключать, процесс мертв."); }
                return Task.FromResult(false);
            }

            try
            {
                lock (_logBuffer) { _logBuffer.Add("[Контроллер]: Принудительное уничтожение процесса..."); }

                _serverProcess.StandardInput.WriteLine("exit");

                lock (_logBuffer) { _logBuffer.Add("[Контроллер]: Процесс успешно уничтожен."); }
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                lock (_logBuffer) { _logBuffer.Add($"[Ошибка при Kill]: {ex.Message}"); }
                return Task.FromResult(false);
            }
        }

        public Task SendCommandAsync(string command)
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                _serverProcess.StandardInput.WriteLine(command);
            }
            return Task.CompletedTask;
        }

        // Метод просто отдает накопленные строки бэкенду панели
        public Task<List<string>> GetLatestLogsAsync()
        {
            lock (_logBuffer)
            {
                return Task.FromResult(_logBuffer.ToList());
            }
        }

        public Task UpdateServerAsync()
        {
            return Task.CompletedTask;
        }
    }
}

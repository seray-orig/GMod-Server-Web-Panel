
using System.Diagnostics;
using System.Reflection;

namespace GMServerWebPanel.API.ServerProcessController.Core
{
    internal sealed class ServerController(string[] args)
    {
        ProcessStartInfo _startInfo = new()
        {
            FileName = args[0],
            Arguments = args[1],
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process _serverProcess = null!;

        public bool Start()
        {
            _serverProcess = new Process { StartInfo = _startInfo };

            _serverProcess.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {

                }
            };

            try
            {
                _serverProcess.Start();
            }
            catch { return  false; }

            _serverProcess.BeginOutputReadLine();

            return true;
        }
    }
}

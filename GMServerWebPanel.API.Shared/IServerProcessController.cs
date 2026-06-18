using System.ServiceModel;

namespace GMServerWebPanel.API.Shared
{
    [ServiceContract(Name = "ServerProcessController")]
    public interface IServerProcessController
    {
        [OperationContract] Task<bool> StartServerAsync();
        [OperationContract] Task<bool> StopServerAsync();
        [OperationContract] Task SendCommandAsync(string command);
        [OperationContract] Task UpdateServerAsync();
        [OperationContract] Task<List<string>> GetLatestLogsAsync();
    }
}

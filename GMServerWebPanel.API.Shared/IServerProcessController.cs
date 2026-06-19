using System.ServiceModel;

namespace GMServerWebPanel.API.Shared
{
    [ServiceContract(Name = "ServerProcessController")]
    public interface IServerProcessController
    {
        [OperationContract]
        Task StartServerAsync();

        [OperationContract]
        Task StopServerAsync();

        [OperationContract]
        Task UpdateServerAsync();

        [OperationContract]
        Task SendCommandAsync(string command);

        [OperationContract]
        IAsyncEnumerable<string> StreamLogsAsync();
    }
}

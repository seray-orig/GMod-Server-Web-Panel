using GMServerWebPanel.API.ServerProcessController.Core;
using ProtoBuf.Grpc.Server;

var builder = WebApplication.CreateBuilder();

builder.WebHost.ConfigureKestrel(options =>
{
    // К сожалению пока хардкод, в будущем сюда можно будет запихнуть любой порт.
    options.ListenLocalhost(50051, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

builder.Services.AddCodeFirstGrpc();

builder.Services.AddSingleton<ServerController>();

var app = builder.Build();

app.MapGrpcService<ServerController>();

Console.WriteLine("Агент успешно запущен и ожидает команды от веб-панели...");

await app.RunAsync("http://localhost:50051");

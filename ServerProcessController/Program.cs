using GMServerWebPanel.API.ServerProcessController.Core;
using ProtoBuf.Grpc.Server;

var builder = WebApplication.CreateBuilder();

builder.WebHost.ConfigureKestrel(options =>
{
    // Настраиваем порт 50051 на обязательное использование HTTP/2 без шифрования (HttpProtocols.Http2)
    options.ListenLocalhost(50051, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

// 1. Включаем Code-First gRPC (который работает по интерфейсам C#)
builder.Services.AddCodeFirstGrpc();

// 2. Регистрируем ваш класс, управляющий процессами srcds.exe и steamcmd
builder.Services.AddSingleton<ServerController>();

var app = builder.Build();

// 3. Привязываем класс к gRPC сети
app.MapGrpcService<ServerController>();

Console.WriteLine("Агент успешно запущен и ожидает команды от веб-панели...");

await app.RunAsync("http://localhost:50051");

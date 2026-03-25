
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SystemStatsService>();

var app = builder.Build();



app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/stats", (SystemStatsService service) =>
{
    return service.GetStats();
});

app.Run();
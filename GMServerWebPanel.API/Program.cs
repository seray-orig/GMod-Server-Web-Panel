using GMServerWebPanel.API.Data;
using GMServerWebPanel.API.Models;
using GMServerWebPanel.API.Services;
using GMServerWebPanel.API.Services.Interfaces;
using GMServerWebPanel.API.Settings;
using GMServerWebPanel.API.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProtoBuf.Grpc.ClientFactory;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Генерация ключа для хеширования JWT токена.
if (string.IsNullOrEmpty(config["JWTSettings:Key"]))
{
    // Каждый раз генерит новый, ибо нефиг. Пусть свой придумывают и записывают в appsettings.json
    config["JWTSettings:Key"] = $"{Guid.NewGuid().ToString()}_{Guid.NewGuid().ToString()}";
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = config["JWTSettings:Issuer"],
        ValidAudience = config["JWTSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWTSettings:Key"]!)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services.Configure<JwtSettings>(config.GetSection("JWTSettings"));
builder.Services.Configure<Argon2Settings>(config.GetSection("Argon2Settings"));

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=users.db"));
builder.Services.AddScoped<ITokenServise, JwtService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordHasher, Argon2Hasher>();
builder.Services.AddScoped<IFileSystemService, LinuxFileSystemService>();
builder.Services.AddSingleton<SystemStatsService>();
builder.Services.AddSignalR();
builder.Services.AddHostedService<LogStreamerService>();

builder.Services.AddCodeFirstGrpcClient<IServerProcessController>(options =>
{
    options.Address = new Uri(config["ServerUrls"] ?? "http://localhost:50051");
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<LogHub>("/hub/logs");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();

    db.Database.EnsureCreated();

    if (!db.Users.Any())
    {
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var fileSystemService = services.GetRequiredService<IFileSystemService>();

        var user = User.GenerateRandomUser();

        fileSystemService.WriteTextFile("gmod-panel-user.txt", $"Admin User\nLogin: {user.Login}\nPassword: {user.Password}");

        user.Password = passwordHasher.HashPassword(user.Password);

        db.Users.Add(user);
        db.SaveChanges();
    }
}

app.Run();

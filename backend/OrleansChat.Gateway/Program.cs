using Microsoft.AspNetCore.SignalR;
using Orleans;
using Orleans.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, UsernameUserIdProvider>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Host.UseOrleansClient(clientBuilder =>
{
    /*SINGLE SILO KONFIGURACIJA
     * clientBuilder.UseLocalhostClustering();
    clientBuilder.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "dev-cluster";
        options.ServiceId = "chat-service";
    });*/

    clientBuilder.UseAdoNetClustering(options =>
    {
        options.Invariant = "Npgsql";
        options.ConnectionString = "Host=localhost;Database=orleans_chat_multisilo;Username=postgres;Password=postgres";
    });

    clientBuilder.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "dev-cluster"; 
        options.ServiceId = "dev-service"; 
    });
});

var app = builder.Build();

app.UseCors();

app.MapHub<OrleansChat.Gateway.AuthHub>("/hub/auth");
app.MapHub<OrleansChat.Gateway.ServerHub>("/hub/server");
app.MapHub<OrleansChat.Gateway.ChatHub>("/hub/chat");

app.Run();
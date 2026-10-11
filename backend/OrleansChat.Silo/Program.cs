using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using System.Net;

namespace OrleansChat.Silo;

public class Program
{
    public static async Task Main(string[] args)
    {
        const string connectionStringSingleSilo = "Host=localhost;Database=orleans_chat;Username=postgres;Password=postgres";
        const string connectionStringMultiSilo = "Host=localhost;Database=orleans_chat_multisilo;Username=postgres;Password=postgres";

        /* SINGLE SILO VARIJANTA
         * var host = Host.CreateDefaultBuilder(args)
            .UseOrleans(siloBuilder =>
            {
                siloBuilder.UseLocalhostClustering(
                    siloPort: 11111,
                    gatewayPort: 30000,
                    primarySiloEndpoint: new IPEndPoint(IPAddress.Loopback, 11111)
                );

                siloBuilder.Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev-cluster";
                    options.ServiceId = "dev-service";
                });

                siloBuilder.AddAdoNetGrainStorage("PostgreSQLStore", options =>
                {
                    options.Invariant = "Npgsql";
                    options.ConnectionString = connectionString;
                });

                siloBuilder.ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                });
            })
            .Build();

        await host.StartAsync();

        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Silo Program.cs");

        logger.LogInformation("========================================================");
        logger.LogInformation("[SILO RUNNING]: Orleans Silo working on port 11111.");
        logger.LogInformation("Press CTRL+C to shut down the server.");
        logger.LogInformation("========================================================");

        await host.WaitForShutdownAsync();
        */

        int siloPort = args.Length > 0 ? int.Parse(args[0]) : 11111;
        int gatewayPort = args.Length > 1 ? int.Parse(args[1]) : 30000;

        var host = Host.CreateDefaultBuilder(args)
            .UseOrleans(siloBuilder =>
            {
                siloBuilder.UseAdoNetClustering(options =>
                {
                    options.Invariant = "Npgsql";
                    options.ConnectionString = connectionStringMultiSilo;
                });

                siloBuilder.ConfigureEndpoints(
                    siloPort: siloPort,
                    gatewayPort: gatewayPort
                );

                siloBuilder.Configure<Orleans.Configuration.EndpointOptions>(options =>
                {
                    options.AdvertisedIPAddress = IPAddress.Loopback;
                });

                siloBuilder.Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev-cluster";
                    options.ServiceId = "dev-service";
                });

                siloBuilder.AddAdoNetGrainStorage("PostgreSQLStore", options =>
                {
                    options.Invariant = "Npgsql";
                    options.ConnectionString = connectionStringMultiSilo;
                });

                siloBuilder.ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                });
            })
            .Build();

        await host.StartAsync();

        // Logovanje portova da znamo koji se pokrenuo
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Silo running on Port: {SiloPort}, Gateway: {GatewayPort}", siloPort, gatewayPort);

        await host.WaitForShutdownAsync();
    }
}
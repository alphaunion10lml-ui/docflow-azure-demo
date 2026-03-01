using DocFlow.Functions.Blob;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureAppConfiguration(config =>
    {
        config.AddEnvironmentVariables();
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((ctx, services) =>
    {
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("BlobClientFactory");
            var options = ctx.Configuration.GetSection("Blob").Get<BlobOptions>() ?? new BlobOptions();

            var serviceClient = BlobClientFactory.Create(options, logger);
            return new DocFlowStorage(serviceClient, options);
        });
    })
    .ConfigureLogging(lb =>
    {
        lb.AddConsole();
    })
    .Build();

// Ensure containers exist (optional; infra creates them in Azure)
using (var scope = host.Services.CreateScope())
{
    var storage = scope.ServiceProvider.GetRequiredService<DocFlowStorage>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    try
    {
        await storage.EnsureContainersExistAsync(CancellationToken.None);
        logger.LogInformation("Storage containers ensured: {Uploads} / {Processed}.", storage.Options.UploadsContainer, storage.Options.ProcessedContainer);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not ensure containers on startup. This might be expected if credentials are missing.");
    }
}

await host.RunAsync();

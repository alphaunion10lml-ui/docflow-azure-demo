using DocFlow.Api.Blob;
using DocFlow.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BlobOptions>(builder.Configuration.GetSection("Blob"));

builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("BlobClientFactory");
    var options = builder.Configuration.GetSection("Blob").Get<BlobOptions>() ?? new BlobOptions();

    var serviceClient = BlobClientFactory.Create(options, logger);
    return new DocFlowStorage(serviceClient, options);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "DocFlow API v1");
    options.RoutePrefix = "swagger"; // Accesible en /swagger
});

// Startup validation: containers should exist.
// In the workshop, infra creates them. Locally, this is convenient.
using (var scope = app.Services.CreateScope())
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

app.MapApiEndpoints();

app.Run();

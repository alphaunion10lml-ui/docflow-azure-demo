using DocFlow.Api.Blob;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DocFlow.Api.HealthChecks;

public class BlobStorageHealthCheck : IHealthCheck
{
    private readonly DocFlowStorage _storage;
    private readonly ILogger<BlobStorageHealthCheck> _logger;

    public BlobStorageHealthCheck(DocFlowStorage storage, ILogger<BlobStorageHealthCheck> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uploadsExists = await _storage.Uploads.ExistsAsync(cancellationToken);
            var processedExists = await _storage.Processed.ExistsAsync(cancellationToken);

            if (!uploadsExists || !processedExists)
            {
                var missingContainers = new List<string>();
                if (!uploadsExists) missingContainers.Add(_storage.Options.UploadsContainer);
                if (!processedExists) missingContainers.Add(_storage.Options.ProcessedContainer);

                return HealthCheckResult.Degraded(
                    $"Some containers are missing: {string.Join(", ", missingContainers)}",
                    data: new Dictionary<string, object>
                    {
                        ["uploads_exists"] = uploadsExists,
                        ["processed_exists"] = processedExists,
                        ["uploads_container"] = _storage.Options.UploadsContainer,
                        ["processed_container"] = _storage.Options.ProcessedContainer
                    });
            }

            var uploadsCount = 0;
            await foreach (var _ in _storage.Uploads.GetBlobsAsync(cancellationToken: cancellationToken).Take(1))
            {
                uploadsCount++;
            }

            return HealthCheckResult.Healthy("Blob storage is accessible and containers exist",
                data: new Dictionary<string, object>
                {
                    ["uploads_container"] = _storage.Options.UploadsContainer,
                    ["processed_container"] = _storage.Options.ProcessedContainer,
                    ["connection_verified"] = true
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for blob storage");
            return HealthCheckResult.Unhealthy(
                "Unable to connect to blob storage",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                });
        }
    }
}

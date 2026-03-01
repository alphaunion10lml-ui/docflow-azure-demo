using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace DocFlow.Functions.Blob;

public static class BlobClientFactory
{
    public static BlobServiceClient Create(BlobOptions options, ILogger logger)
    {
        var mode = (options.Mode ?? string.Empty).Trim();

        if (string.Equals(mode, "ManagedIdentity", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.ServiceUri))
                throw new InvalidOperationException("Blob__ServiceUri is required when Blob__Mode=ManagedIdentity.");

            logger.LogInformation("Creating BlobServiceClient using Managed Identity (DefaultAzureCredential) against {ServiceUri}.", options.ServiceUri);

            var uri = new Uri(options.ServiceUri);
            return new BlobServiceClient(uri, new DefaultAzureCredential());
        }

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new InvalidOperationException("Blob__ConnectionString is required when Blob__Mode=ConnectionString (or when Mode is omitted).");

        logger.LogInformation("Creating BlobServiceClient using Connection String.");
        return new BlobServiceClient(options.ConnectionString);
    }
}

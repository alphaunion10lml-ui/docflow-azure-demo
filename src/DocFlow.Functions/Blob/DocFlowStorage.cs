using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace DocFlow.Functions.Blob;

public sealed class DocFlowStorage
{
    public DocFlowStorage(BlobServiceClient serviceClient, BlobOptions options)
    {
        ServiceClient = serviceClient;
        Options = options;

        Uploads = ServiceClient.GetBlobContainerClient(options.UploadsContainer);
        Processed = ServiceClient.GetBlobContainerClient(options.ProcessedContainer);
    }

    public BlobServiceClient ServiceClient { get; }
    public BlobOptions Options { get; }

    public BlobContainerClient Uploads { get; }
    public BlobContainerClient Processed { get; }

    public async Task EnsureContainersExistAsync(CancellationToken ct)
    {
        await Uploads.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        await Processed.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
    }
}

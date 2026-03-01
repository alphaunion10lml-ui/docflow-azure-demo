using System.Security.Cryptography;
using System.Text.Json;
using Azure.Storage.Blobs.Models;
using DocFlow.Functions.Blob;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DocFlow.Functions;

public sealed class ProcessUploadFunction(DocFlowStorage storage, ILoggerFactory loggerFactory)
{
    private readonly DocFlowStorage _storage = storage;
    private readonly ILogger _log = loggerFactory.CreateLogger<ProcessUploadFunction>();
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    [Function("ProcessUpload")]
    public async Task Run(
        [BlobTrigger("uploads/{name}", Connection = "DocFlowStorage")] Stream blobStream,
        string name,
        CancellationToken ct)
    {
        var uploadBlobName = name; // includes yyyy/MM/dd/... segments
        var uploadBlob = _storage.Uploads.GetBlobClient(uploadBlobName);

        _log.LogInformation("Blob trigger fired. uploads/{BlobName}", uploadBlobName);

        // We prefer metadata (authoritative), but can fall back to parsing the fileId from the name.
        string fileId = "unknown";
        BlobProperties props;

        try
        {
            props = (await uploadBlob.GetPropertiesAsync(cancellationToken: ct)).Value;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to read blob properties for uploads/{BlobName}.", uploadBlobName);
            return;
        }

        if (props.Metadata.TryGetValue("docflow-fileid", out var mid) && !string.IsNullOrWhiteSpace(mid))
        {
            fileId = mid;
        }
        else if (BlobName.TryExtractFileIdFromUploadBlobName(uploadBlobName, out var parsed))
        {
            fileId = parsed;
        }

        var contentType = props.ContentType;
        var sizeBytes = props.ContentLength;

        try
        {
            // Compute SHA256
            var sha256 = await ComputeSha256Async(blobStream, ct);

            var payload = new
            {
                fileId,
                source = new
                {
                    container = _storage.Options.UploadsContainer,
                    blobName = uploadBlobName
                },
                analysis = new
                {
                    sha256,
                    sizeBytes,
                    contentType
                },
                processedAtUtc = DateTimeOffset.UtcNow
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            // Write result
            var resultBlobName = BlobName.ProcessedBlobName(fileId);
            var resultBlob = _storage.Processed.GetBlobClient(resultBlobName);

            _log.LogInformation("Writing processed result: {Container}/{BlobName}", _storage.Options.ProcessedContainer, resultBlobName);

            await resultBlob.UploadAsync(BinaryData.FromString(json), overwrite: true, cancellationToken: ct);

            await resultBlob.SetHttpHeadersAsync(new BlobHttpHeaders { ContentType = "application/json" }, cancellationToken: ct);

            // Update metadata
            var newMetadata = new Dictionary<string, string>(props.Metadata, StringComparer.OrdinalIgnoreCase)
            {
                ["docflow-status"] = "processed",
                ["docflow-processedat"] = DateTimeOffset.UtcNow.ToString("O")
            };

            await uploadBlob.SetMetadataAsync(newMetadata, cancellationToken: ct);

            _log.LogInformation("Done. fileId={FileId}, status=processed.", fileId);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Processing failed for uploads/{BlobName} (fileId={FileId}).", uploadBlobName, fileId);

            try
            {
                var newMetadata = new Dictionary<string, string>(props.Metadata, StringComparer.OrdinalIgnoreCase)
                {
                    ["docflow-status"] = "failed",
                    ["docflow-error"] = Truncate(ex.GetType().Name + ": " + ex.Message, 700)
                };
                await uploadBlob.SetMetadataAsync(newMetadata, cancellationToken: ct);
            }
            catch (Exception ex2)
            {
                _log.LogWarning(ex2, "Failed to set failure metadata for uploads/{BlobName}.", uploadBlobName);
            }
        }
    }

    private static async Task<string> ComputeSha256Async(Stream stream, CancellationToken ct)
    {
        // The trigger stream is positioned at start and is read-once.
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Truncate(string value, int maxLen)
        => value.Length <= maxLen ? value : value[..maxLen];
}

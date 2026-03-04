using Azure.Storage.Blobs.Models;
using DocFlow.Api.Blob;
using DocFlow.Api.Models;
using DocFlow.Api.Services;
using System.Diagnostics;

namespace DocFlow.Api.Endpoints;

public static class FileEndpoints
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/files")
            .WithTags("Files");

        group.MapPost("/", UploadFileAsync)
            .Accepts<IFormFile>("multipart/form-data")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .DisableAntiforgery();

        group.MapGet("/", ListFilesAsync)
            .Produces(StatusCodes.Status200OK);

        group.MapGet("/{fileId}/result", GetFileResultAsync)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> UploadFileAsync(
        HttpRequest request,
        DocFlowStorage storage,
        ILoggerFactory loggerFactory,
        ITelemetryService telemetry,
        CancellationToken ct)
    {
        var log = loggerFactory.CreateLogger("FileEndpoints");
        var sw = Stopwatch.StartNew();
        var success = false;

        try
        {
            if (!request.HasFormContentType)
                return Results.BadRequest(new { error = "Expected multipart/form-data" });

            var form = await request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
            if (file is null)
                return Results.BadRequest(new { error = "Missing file. Provide a multipart field named 'file'." });

            var fileId = Guid.NewGuid().ToString("N");
            var utcNow = DateTimeOffset.UtcNow;
            var blobName = BlobName.NewUploadBlobName(utcNow, fileId, file.FileName);

            var blobClient = storage.Uploads.GetBlobClient(blobName);

            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["docflow_status"] = "pending",
                ["docflow_fileid"] = fileId
            };

            var headers = new BlobHttpHeaders
            {
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType
            };

            log.LogInformation("Uploading fileId={FileId} as blob {BlobName} ({ContentType}, {Length} bytes).",
                fileId, blobName, headers.ContentType, file.Length);

            await using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = headers,
                Metadata = metadata
            }, ct);

            success = true;
            sw.Stop();

            telemetry.TrackFileUpload(fileId, file.FileName, file.Length, headers.ContentType, success, sw.Elapsed);

            return Results.Ok(new
            {
                fileId,
                blobName,
                status = "pending",
                uploadedAtUtc = utcNow
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            log.LogError(ex, "Error uploading file");
            telemetry.TrackFileUpload("error", "unknown", 0, "unknown", false, sw.Elapsed);
            throw;
        }
    }

    private static async Task<IResult> ListFilesAsync(
        DocFlowStorage storage,
        ITelemetryService telemetry,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var results = new List<FileListItem>();

        await foreach (var item in storage.Uploads.GetBlobsAsync(new GetBlobsOptions { Traits = BlobTraits.Metadata }, ct))
        {
            var blobName = item.Name;
            var fileId = item.Metadata != null && item.Metadata.TryGetValue("docflow_fileid", out var mid) ? mid : null;

            if (string.IsNullOrWhiteSpace(fileId) && BlobName.TryExtractFileIdFromUploadBlobName(blobName, out var extracted))
                fileId = extracted;

            fileId ??= "unknown";

            var status = item.Metadata != null && item.Metadata.TryGetValue("docflow_status", out var st) ? st : "unknown";

            var processedBlob = storage.Processed.GetBlobClient(BlobName.ProcessedBlobName(fileId));
            var processedExists = await processedBlob.ExistsAsync(ct);

            var resultUrl = processedExists ? $"/files/{Uri.EscapeDataString(fileId)}/result" : null;

            results.Add(new FileListItem(
                FileId: fileId,
                BlobName: blobName,
                OriginalName: BlobName.TryExtractOriginalNameFromUploadBlobName(blobName),
                Status: status,
                ContentType: item.Properties.ContentType,
                SizeBytes: item.Properties.ContentLength,
                CreatedOnUtc: item.Properties.CreatedOn,
                ResultUrl: resultUrl
            ));
        }

        sw.Stop();
        telemetry.TrackFileList(results.Count, sw.Elapsed);

        return Results.Ok(results.OrderByDescending(x => x.CreatedOnUtc ?? DateTimeOffset.MinValue));
    }

    private static async Task<IResult> GetFileResultAsync(
        string fileId,
        DocFlowStorage storage,
        ITelemetryService telemetry,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var blobClient = storage.Processed.GetBlobClient(BlobName.ProcessedBlobName(fileId));

        var exists = await blobClient.ExistsAsync(ct);
        sw.Stop();

        telemetry.TrackFileResultRetrieval(fileId, exists, sw.Elapsed);

        if (!exists)
            return Results.NotFound(new { error = $"Result not found for fileId={fileId}" });

        var download = await blobClient.DownloadContentAsync(ct);
        return Results.Content(download.Value.Content.ToString(), "application/json");
    }
}

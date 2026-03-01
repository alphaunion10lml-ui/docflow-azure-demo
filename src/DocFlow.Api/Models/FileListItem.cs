namespace DocFlow.Api.Models;

public sealed record FileListItem(
    string FileId,
    string BlobName,
    string OriginalName,
    string Status,
    string? ContentType,
    long? SizeBytes,
    DateTimeOffset? CreatedOnUtc,
    string? ResultUrl
);

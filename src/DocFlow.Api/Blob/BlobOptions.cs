namespace DocFlow.Api.Blob;

public sealed class BlobOptions
{
    /// <summary>
    /// ConnectionString | ManagedIdentity
    /// </summary>
    public string? Mode { get; init; }

    public string? ConnectionString { get; init; }
    public string? ServiceUri { get; init; }

    public string UploadsContainer { get; init; } = "uploads";
    public string ProcessedContainer { get; init; } = "processed";
}

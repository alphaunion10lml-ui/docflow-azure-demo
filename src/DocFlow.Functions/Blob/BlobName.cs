namespace DocFlow.Functions.Blob;

public static class BlobName
{
    public static string ProcessedBlobName(string fileId) => $"{fileId}.json";

    public static bool TryExtractFileIdFromUploadBlobName(string uploadBlobName, out string fileId)
    {
        // upload blob format: yyyy/MM/dd/{fileId}-{originalName}
        fileId = string.Empty;
        var lastSegment = uploadBlobName.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (string.IsNullOrWhiteSpace(lastSegment))
            return false;

        var dashIdx = lastSegment.IndexOf('-', StringComparison.Ordinal);
        if (dashIdx <= 0)
            return false;

        fileId = lastSegment[..dashIdx];
        return !string.IsNullOrWhiteSpace(fileId);
    }
}

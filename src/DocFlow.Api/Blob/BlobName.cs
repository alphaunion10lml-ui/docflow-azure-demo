using System.Text.RegularExpressions;

namespace DocFlow.Api.Blob;

public static class BlobName
{
    private static readonly Regex InvalidFileNameChars = new(@"[^a-zA-Z0-9._\- ]+", RegexOptions.Compiled);

    public static string NewUploadBlobName(DateTimeOffset utcNow, string fileId, string originalName)
    {
        var safeName = SanitizeFileName(originalName);
        return $"{utcNow:yyyy}/{utcNow:MM}/{utcNow:dd}/{fileId}-{safeName}";
    }

    public static string ProcessedBlobName(string fileId) => $"{fileId}.json";

    public static string SanitizeFileName(string name)
    {
        name = Path.GetFileName(name);
        name = name.Replace(" ", "_");
        name = InvalidFileNameChars.Replace(name, "");
        name = name.Trim('_');

        // Keep it reasonably short for demo purposes.
        if (name.Length > 120)
            name = name[..120];

        return string.IsNullOrWhiteSpace(name) ? "file" : name;
    }

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

    public static string TryExtractOriginalNameFromUploadBlobName(string uploadBlobName)
    {
        var lastSegment = uploadBlobName.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (string.IsNullOrWhiteSpace(lastSegment))
            return uploadBlobName;

        var dashIdx = lastSegment.IndexOf('-', StringComparison.Ordinal);
        if (dashIdx <= 0 || dashIdx + 1 >= lastSegment.Length)
            return lastSegment;

        return lastSegment[(dashIdx + 1)..];
    }
}

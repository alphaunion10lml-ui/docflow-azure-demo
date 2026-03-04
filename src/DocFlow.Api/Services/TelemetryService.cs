using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace DocFlow.Api.Services;

public interface ITelemetryService
{
    void TrackFileUpload(string fileId, string fileName, long fileSize, string contentType, bool success, TimeSpan duration);
    void TrackFileList(int fileCount, TimeSpan duration);
    void TrackFileResultRetrieval(string fileId, bool found, TimeSpan duration);
    IDisposable StartOperation(string operationName);
}

public class TelemetryService : ITelemetryService
{
    private readonly TelemetryClient? _telemetryClient;
    private readonly ILogger<TelemetryService> _logger;

    public TelemetryService(TelemetryClient? telemetryClient, ILogger<TelemetryService> logger)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    public void TrackFileUpload(string fileId, string fileName, long fileSize, string contentType, bool success, TimeSpan duration)
    {
        var properties = new Dictionary<string, string>
        {
            ["FileId"] = fileId,
            ["FileName"] = fileName,
            ["ContentType"] = contentType,
            ["Success"] = success.ToString()
        };

        var metrics = new Dictionary<string, double>
        {
            ["FileSizeBytes"] = fileSize,
            ["DurationMs"] = duration.TotalMilliseconds
        };

        _telemetryClient?.TrackEvent("FileUploaded", properties, metrics);
        
        _logger.LogInformation(
            "File upload tracked: FileId={FileId}, FileName={FileName}, Size={FileSize}, Success={Success}, Duration={Duration}ms",
            fileId, fileName, fileSize, success, duration.TotalMilliseconds);
    }

    public void TrackFileList(int fileCount, TimeSpan duration)
    {
        var metrics = new Dictionary<string, double>
        {
            ["FileCount"] = fileCount,
            ["DurationMs"] = duration.TotalMilliseconds
        };

        _telemetryClient?.TrackEvent("FileListRetrieved", metrics: metrics);
        
        _logger.LogInformation(
            "File list retrieved: Count={FileCount}, Duration={Duration}ms",
            fileCount, duration.TotalMilliseconds);
    }

    public void TrackFileResultRetrieval(string fileId, bool found, TimeSpan duration)
    {
        var properties = new Dictionary<string, string>
        {
            ["FileId"] = fileId,
            ["Found"] = found.ToString()
        };

        var metrics = new Dictionary<string, double>
        {
            ["DurationMs"] = duration.TotalMilliseconds
        };

        _telemetryClient?.TrackEvent("FileResultRetrieved", properties, metrics);
        
        _logger.LogInformation(
            "File result retrieval: FileId={FileId}, Found={Found}, Duration={Duration}ms",
            fileId, found, duration.TotalMilliseconds);
    }

    public IDisposable StartOperation(string operationName)
    {
        if (_telemetryClient != null)
        {
            return _telemetryClient.StartOperation<RequestTelemetry>(operationName);
        }

        return new NoOpDisposable();
    }

    private class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}

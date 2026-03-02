using Azure.Storage.Blobs.Models;
using DocFlow.Api.Blob;
using DocFlow.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BlobOptions>(builder.Configuration.GetSection("Blob"));

builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("BlobClientFactory");
    var options = builder.Configuration.GetSection("Blob").Get<BlobOptions>() ?? new BlobOptions();

    var serviceClient = BlobClientFactory.Create(options, logger);
    return new DocFlowStorage(serviceClient, options);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Startup validation: containers should exist.
// In the workshop, infra creates them. Locally, this is convenient.
using (var scope = app.Services.CreateScope())
{
    var storage = scope.ServiceProvider.GetRequiredService<DocFlowStorage>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        await storage.EnsureContainersExistAsync(CancellationToken.None);
        logger.LogInformation("Storage containers ensured: {Uploads} / {Processed}.", storage.Options.UploadsContainer, storage.Options.ProcessedContainer);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not ensure containers on startup. This might be expected if credentials are missing.");
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTimeOffset.UtcNow }))
   .WithName("Health");

app.MapPost("/files", async (HttpRequest request, DocFlowStorage storage, ILoggerFactory loggerFactory, CancellationToken ct) =>
{
    var log = loggerFactory.CreateLogger("POST /files");

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

    log.LogInformation("Uploading fileId={FileId} as blob {BlobName} ({ContentType}, {Length} bytes).", fileId, blobName, headers.ContentType, file.Length);

    await using var stream = file.OpenReadStream();
    await blobClient.UploadAsync(stream, new BlobUploadOptions
    {
        HttpHeaders = headers,
        Metadata = metadata
    }, ct);

    return Results.Ok(new
    {
        fileId,
        blobName,
        status = "pending",
        uploadedAtUtc = utcNow
    });
})
.Accepts<IFormFile>("multipart/form-data")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

app.MapGet("/files", async (DocFlowStorage storage, CancellationToken ct) =>
{
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

    return Results.Ok(results.OrderByDescending(x => x.CreatedOnUtc ?? DateTimeOffset.MinValue));
})
.Produces(StatusCodes.Status200OK);

app.MapGet("/files/{fileId}/result", async (string fileId, DocFlowStorage storage, CancellationToken ct) =>
{
    var blobClient = storage.Processed.GetBlobClient(BlobName.ProcessedBlobName(fileId));

    if (!await blobClient.ExistsAsync(ct))
        return Results.NotFound(new { error = $"Result not found for fileId={fileId}" });

    var download = await blobClient.DownloadContentAsync(ct);
    return Results.Content(download.Value.Content.ToString(), "application/json");
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/", () =>
{
    // Simple UI to avoid any front-end framework (focus is Azure).
    var html = """
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8"/>
  <meta name="viewport" content="width=device-width, initial-scale=1"/>
  <title>DocFlow</title>
  <style>
    body{font-family:system-ui,-apple-system,Segoe UI,Roboto,Arial,sans-serif;margin:2rem;max-width:900px}
    .row{display:flex;gap:1rem;align-items:center;flex-wrap:wrap}
    .card{border:1px solid #ddd;border-radius:12px;padding:1rem;margin-top:1rem}
    table{width:100%;border-collapse:collapse}
    th,td{border-bottom:1px solid #eee;text-align:left;padding:.5rem}
    code{background:#f6f8fa;padding:.1rem .3rem;border-radius:6px}
    small{color:#555}
  </style>
</head>
<body>
  <h1>DocFlow</h1>
  <p><small>Demo minimal para <code>App Service + Blob Storage + Functions + Key Vault</code>.</small></p>

  <div class="card">
    <h2>Subir archivo</h2>
    <form id="uploadForm">
      <div class="row">
        <input type="file" id="file" name="file" required/>
        <button type="submit">Subir</button>
      </div>
    </form>
    <pre id="uploadResult"></pre>
  </div>

  <div class="card">
    <div class="row" style="justify-content:space-between">
      <h2 style="margin:0">Archivos</h2>
      <button id="refresh">Refrescar</button>
    </div>
    <table>
      <thead>
        <tr>
          <th>FileId</th>
          <th>Nombre</th>
          <th>Status</th>
          <th>Resultado</th>
        </tr>
      </thead>
      <tbody id="tbody"></tbody>
    </table>
  </div>

<script>
const tbody = document.getElementById('tbody');
const uploadForm = document.getElementById('uploadForm');
const uploadResult = document.getElementById('uploadResult');

async function loadFiles(){
  tbody.innerHTML = '<tr><td colspan="4">Cargando...</td></tr>';
  const res = await fetch('/files');
  const data = await res.json();
  tbody.innerHTML = '';
  if (!data.length){
    tbody.innerHTML = '<tr><td colspan="4">Sin archivos aún</td></tr>';
    return;
  }
  for (const f of data){
    const tr = document.createElement('tr');
    tr.innerHTML = `
      <td><code>${f.fileId}</code></td>
      <td>${f.originalName ?? f.blobName}</td>
      <td>${f.status}</td>
      <td>${f.resultUrl ? `<a href="${f.resultUrl}" target="_blank">ver JSON</a>` : '-'}</td>
    `;
    tbody.appendChild(tr);
  }
}

uploadForm.addEventListener('submit', async (e) => {
  e.preventDefault();
  uploadResult.textContent = 'Subiendo...';
  const fileInput = document.getElementById('file');
  if (!fileInput.files.length) return;

  const fd = new FormData();
  fd.append('file', fileInput.files[0]);

  const res = await fetch('/files', { method: 'POST', body: fd });
  const data = await res.json();
  uploadResult.textContent = JSON.stringify(data, null, 2);
  await loadFiles();
});

document.getElementById('refresh').addEventListener('click', loadFiles);

loadFiles();
</script>
</body>
</html>
""";
    return Results.Content(html, "text/html; charset=utf-8");
});

app.Run();

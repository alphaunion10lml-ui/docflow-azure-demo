namespace DocFlow.Api.Endpoints;

public static class HomeEndpoints
{
    public static IEndpointRouteBuilder MapHomeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () =>
        {
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
        })
        .WithName("Home")
        .ExcludeFromDescription();

        return app;
    }
}

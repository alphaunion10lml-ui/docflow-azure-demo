# 02 - Azure Blob Storage

Objetivo del módulo:
- Ver containers `uploads` y `processed`
- Revisar naming convention + metadata
- Validar el output del procesamiento

---

## 1) Containers esperados
En tu Storage Account → Containers:
- `uploads`
- `processed`

---

## 2) Validar uploads desde la API

1. Ve al Web App (DocFlow UI) y sube un archivo.
2. En `uploads`, deberías ver un blob con el formato:

```
yyyy/MM/dd/{fileId}-{originalName}
```

3. En **Metadata** del blob:
- `docflow-status = pending` (luego `processed`)
- `docflow-fileid = <fileId>`

---

## 3) Validar processed

Tras unos segundos (cuando la Function procese el blob), en container `processed` debe aparecer:

```
{fileId}.json
```

Ábrelo y valida campos como:
- `analysis.sha256`
- `analysis.sizeBytes`
- `analysis.contentType`
- `processedAtUtc`

---

## 4) Troubleshooting rápido
- Si el blob queda en `pending` mucho tiempo:
  - revisa si la Function App está desplegada
  - revisa logs de Functions (Runbooks 03)

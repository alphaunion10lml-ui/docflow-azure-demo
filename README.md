# docflow-azure-demo

Repo demo listo para talleres (teoría + práctica) sobre:

- **Azure App Service (Web Apps)**: aloja la API (y UI HTML simple).
- **Azure Blob Storage**: containers `uploads` y `processed`.
- **Azure Functions**: Blob Trigger que procesa automáticamente un upload y genera un JSON de resultado.
- **Azure Key Vault**: secretos y Key Vault references (y opcional modo Managed Identity).

---

## Arquitectura (E2E)

1) Usuario sube un archivo a la API (App Service)

- `POST /files` (multipart/form-data)
- La API lo guarda en Blob Storage:

```
uploads/{yyyy}/{MM}/{dd}/{fileId}-{originalName}
metadata:
  docflow-status = pending
  docflow-fileid = <fileId>
```

2) Azure Function (Blob Trigger) se activa:

- Lee el blob, calcula:
  - SHA256
  - sizeBytes
  - contentType
  - timestamp
- Escribe el resultado:

```
processed/{fileId}.json
```

- Actualiza metadata del blob original:
  - `docflow-status = processed` (o `failed`)

3) API lista archivos y permite ver el resultado:

- `GET /files`
- `GET /files/{fileId}/result`

---

## Estructura del repo

- `src/DocFlow.Api` → .NET 10 Minimal API
- `src/DocFlow.Functions` → .NET 8 Isolated Worker (Blob Trigger)
- `infra/` → scripts AZ CLI para crear recursos + app settings
- `runbooks/` → guías paso a paso para los talleres
- `ENV.md` → variables de entorno / app settings

---

## Quickstart (Azure)

### 1) Crear recursos (AZ CLI)
```bash
cd infra
chmod +x deploy.sh
./deploy.sh -p demo -l eastus -g rg-demo-docflow
```

> Esto crea: RG, Storage + containers, Key Vault + secret, Web App + Function App, Managed Identities + permisos mínimos, y app settings (incluyendo Key Vault references).

### 2) Deploy API (Web App)
Sigue: `runbooks/01-app-service.md`

### 3) Deploy Functions (Function App)
Sigue: `runbooks/03-functions.md`

### 4) Validar flujo completo
- Abre la Web App y sube un archivo desde `/`
- Revisa que en `processed` aparezca `{fileId}.json`
- Desde la API, revisa `GET /files/{fileId}/result`

---

## Modo Managed Identity (opcional “pro”)

Puedes cambiar ambos apps a Managed Identity para acceso a blobs (sin secretos) con:

```bash
cd infra
chmod +x switch-to-managed-identity.sh
./switch-to-managed-identity.sh -g <RG> -w <WEBAPP> -f <FUNCAPP> -s <STORAGE>
```

> Ver detalles en `ENV.md`.

---

## Runbooks

- `runbooks/01-app-service.md`
- `runbooks/02-blob-storage.md`
- `runbooks/03-functions.md`
- `runbooks/04-key-vault.md`

---

## Nota sobre runtimes

- API: `.NET 10` (App Service Linux runtime stack suele ser `DOTNETCORE|10.0`).
- Functions: `.NET 8 isolated` (Functions runtime v4).

Si tu región/stack varía, revisa el runtime stack en Portal (Configuration → General settings) y ajusta el script.

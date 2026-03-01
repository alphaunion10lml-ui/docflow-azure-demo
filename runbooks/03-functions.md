# 03 - Azure Functions

Objetivo del módulo:
- Desplegar `DocFlow.Functions` a un Function App
- Ver logs de ejecución del Blob Trigger
- Validar que se genera `processed/{fileId}.json`

---

## 1) Prerrequisitos

- Function App creado (por `infra/deploy.sh`)
- Containers `uploads` / `processed` existen
- App Settings incluyen:
  - `DocFlowStorage` (connection string o identity-based connection)
  - `Blob__...` (modo A o B)
  - `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`

---

## 2) Deploy recomendado: GitHub Actions con Publish Profile

### 2.1 Obtener Publish Profile
Portal → Function App → **Get publish profile**

### 2.2 Configurar Secrets/Variables en GitHub
En tu repo (Settings → Secrets and variables → Actions):

**Secrets**
- `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` = contenido XML del publish profile

**Variables**
- `AZURE_FUNCTIONAPP_NAME` = nombre del Function App (ej: `func-demo-df-123456`)

### 2.3 Ejecutar workflow
Actions → `deploy-functions` → Run workflow

---

## 3) Validar trigger

1. Sube un archivo a través de la API (Web App UI)
2. Ve a Function App → **Functions** → `ProcessUpload`
3. Revisa:
   - **Monitor** (invocations)
   - **Log stream**

Logs esperados:
- `Blob trigger fired. uploads/yyyy/MM/dd/...`
- `Writing processed result: processed/{fileId}.json`
- `Done. fileId=..., status=processed.`

---

## 4) Identity-based connection (opcional)

Si quieres que el trigger use Managed Identity en vez de connection string:
- setea:
  - `DocFlowStorage__blobServiceUri`
  - `DocFlowStorage__queueServiceUri`

y asegúrate que el Function App identity tenga RBAC:
- `Storage Blob Data Contributor`
- `Storage Queue Data Contributor`

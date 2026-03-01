# DocFlow - Variables de entorno / App Settings

DocFlow soporta **2 modos de conexión a Blob Storage**, sin cambios de código (solo App Settings).

> Convención de nombres:
> - En App Service / Function App, usa `Blob__X` (doble guion bajo) para mapear a `Blob:X` en .NET.

---

## Config común (API + Functions)

### Containers
- `Blob__UploadsContainer` (default: `uploads`)
- `Blob__ProcessedContainer` (default: `processed`)

### Modo A: Connection String (recomendado para demo Key Vault references)
- `Blob__Mode=ConnectionString`
- `Blob__ConnectionString=<storage-connection-string>`

> Ideal: `Blob__ConnectionString` se setea como **Key Vault reference** a `docflow-storage-connectionstring`.

### Modo B: Managed Identity (modo pro)
- `Blob__Mode=ManagedIdentity`
- `Blob__ServiceUri=https://<storage>.blob.core.windows.net`

> Requiere:
> - Identity habilitada en Web App / Function App
> - Roles RBAC en el Storage Account (mínimo):
>   - Web App: `Storage Blob Data Contributor`
>   - Function App: `Storage Blob Data Contributor` (+ para triggers con MI: `Storage Queue Data Contributor`)

---

## Config específica: Azure Functions (Blob Trigger)

El trigger está definido como:

- Path: `uploads/{name}`
- Connection: `DocFlowStorage`

### Opción 1 (simple): connection string (Key Vault reference)
- `DocFlowStorage=<storage-connection-string>`
- `AzureWebJobsStorage=<storage-connection-string>`

> En esta demo, ambos se setean por Key Vault reference en el script `infra/deploy.sh`.

### Opción 2 (pro): identity-based connection (sin secretos)
Para el blob trigger, hay que setear **blob + queue** URIs:

- `DocFlowStorage__blobServiceUri=https://<storage>.blob.core.windows.net`
- `DocFlowStorage__queueServiceUri=https://<storage>.queue.core.windows.net`

> Nota: el host de Functions aún requiere una configuración para `AzureWebJobsStorage`. En escenarios “sin secretos” se puede configurar también por identity-based settings, pero para este repo lo dejamos como opcional avanzado.

---

## Key Vault

Secret esperado:
- Nombre: `docflow-storage-connectionstring`
- Valor: Storage Account Connection String

Key Vault reference en App Settings:
- `@Microsoft.KeyVault(SecretUri=<secret-uri-con-version>)`

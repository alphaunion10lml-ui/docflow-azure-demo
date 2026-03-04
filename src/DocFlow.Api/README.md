# DocFlow API

API RESTful para gestión de documentos con Azure Blob Storage, Azure Functions y Application Insights.

## 🚀 Inicio Rápido

### Prerrequisitos
- .NET 10
- Azure Storage Account
- Azure Application Insights (opcional para desarrollo)

### Configuración

1. **Clonar el repositorio**
```bash
git clone <repository-url>
cd docflow-azure-demo
```

2. **Configurar secretos locales**
```bash
cd src/DocFlow.Api
dotnet user-secrets set "Blob:ConnectionString" "YOUR_STORAGE_CONNECTION_STRING"
dotnet user-secrets set "ApplicationInsights:ConnectionString" "YOUR_APPINSIGHTS_CONNECTION_STRING"
```

3. **Ejecutar la aplicación**
```bash
dotnet run
```

4. **Acceder a la aplicación**
- UI: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- Health: http://localhost:5000/health

## 📁 Estructura del Proyecto

```
DocFlow.Api/
├── Endpoints/              # Minimal API endpoints organizados
│   ├── HealthEndpoints.cs  # Health checks
│   ├── FileEndpoints.cs    # Operaciones de archivos
│   ├── HomeEndpoints.cs    # UI
│   └── EndpointExtensions.cs
├── Services/               # Servicios de la aplicación
│   └── TelemetryService.cs # Application Insights
├── HealthChecks/           # Health checks personalizados
│   └── BlobStorageHealthCheck.cs
├── Blob/                   # Lógica de Azure Storage
│   ├── BlobClientFactory.cs
│   ├── DocFlowStorage.cs
│   ├── BlobName.cs
│   └── BlobOptions.cs
└── Models/                 # Modelos de datos
    └── FileListItem.cs
```

## 🔌 Endpoints

### Files
- `POST /files` - Subir archivo
- `GET /files` - Listar archivos
- `GET /files/{fileId}/result` - Obtener resultado procesado

### Health
- `GET /health` - Estado completo con detalles
- `GET /health/live` - Liveness probe
- `GET /health/ready` - Readiness probe (incluye dependencias)

### UI
- `GET /` - Interfaz de usuario simple

## 🏥 Health Checks

Los health checks verifican:
- ✅ Conectividad con Azure Blob Storage
- ✅ Existencia de contenedores (`uploads`, `processed`)
- ✅ Capacidad de lectura/escritura

### Ejemplo de respuesta
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "totalDuration": 45.2,
  "checks": [
    {
      "name": "blob_storage",
      "status": "Healthy",
      "description": "Blob storage is accessible and containers exist",
      "duration": 43.5,
      "data": {
        "uploads_container": "uploads",
        "processed_container": "processed",
        "connection_verified": true
      }
    }
  ]
}
```

## 📊 Observabilidad

### Application Insights
La aplicación registra automáticamente:
- **Custom Events:**
  - `FileUploaded` - Con métricas de tamaño y duración
  - `FileListRetrieved` - Cantidad de archivos
  - `FileResultRetrieved` - Éxito/fallo

- **Dependencies:**
  - Azure Blob Storage calls
  - Duración y estado

- **Exceptions:**
  - Stack traces completos
  - Contexto de la operación

### Logs Estructurados
```csharp
log.LogInformation(
    "Uploading fileId={FileId} as blob {BlobName} ({ContentType}, {Length} bytes).",
    fileId, blobName, headers.ContentType, file.Length);
```

## ⚙️ Configuración

### appsettings.json
```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=..."
  },
  "Blob": {
    "Mode": "ConnectionString",
    "ConnectionString": "DefaultEndpointsProtocol=https;...",
    "UploadsContainer": "uploads",
    "ProcessedContainer": "processed"
  }
}
```

### Variables de Entorno (Azure App Service)
```bash
APPLICATIONINSIGHTS__CONNECTIONSTRING=<your-connection-string>
BLOB__CONNECTIONSTRING=<your-storage-connection-string>
```

## 🧪 Testing

### Con cURL
```bash
# Health check
curl http://localhost:5000/health

# Upload file
curl -X POST http://localhost:5000/files \
  -F "file=@test.pdf"

# List files
curl http://localhost:5000/files

# Get result
curl http://localhost:5000/files/{fileId}/result
```

### Con Swagger
Navega a `http://localhost:5000/swagger` para probar la API interactivamente.

## 🐳 Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/DocFlow.Api/DocFlow.Api.csproj", "src/DocFlow.Api/"]
RUN dotnet restore "src/DocFlow.Api/DocFlow.Api.csproj"
COPY . .
WORKDIR "/src/src/DocFlow.Api"
RUN dotnet build "DocFlow.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DocFlow.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocFlow.Api.dll"]
```

## 🚢 Deployment

### Azure App Service
1. Crear App Service (Linux, .NET 10)
2. Configurar Application Settings con las connection strings
3. Habilitar Application Insights
4. Desplegar desde GitHub Actions o Azure DevOps

### Health Check Configuration
```bash
# Azure App Service Health Check Path
/health/ready
```

## 📚 Documentación Adicional

- [Guía de Mejoras Detalladas](../../docs/IMPROVEMENTS.md)
- [Azure Storage Documentation](https://learn.microsoft.com/azure/storage/blobs/)
- [Application Insights](https://learn.microsoft.com/azure/azure-monitor/app/asp-net-core)

## 🤝 Contribuir

1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## 📄 Licencia

Este proyecto está bajo la licencia MIT.

---

**Nota:** Asegúrate de no commitear connection strings o secretos en el repositorio. Usa `dotnet user-secrets` para desarrollo local y variables de entorno en producción.

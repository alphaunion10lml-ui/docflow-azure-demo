# 01 - Azure App Service (Web Apps)

Objetivo del módulo:
- Desplegar la **API DocFlow** a un **Web App**
- Configurar **App Settings** (incluyendo Key Vault reference)
- Ver logs (Log stream) y validar endpoints

---

## 1) Prerrequisitos

- Recursos creados con `infra/deploy.sh` (o manual)
- Un Web App (Linux) creado (ej: `app-<prefix>-df-<suffix>`)
- App Settings ya configurados (el script los configura)

---

## 2) Deploy recomendado (sin build local): GitHub Actions con Publish Profile

### 2.1 Obtener Publish Profile
1. Portal → Web App → **Get publish profile**
2. Copia el contenido (XML)

### 2.2 Configurar Secrets/Variables en GitHub
En tu repo (Settings → Secrets and variables → Actions):

**Secrets**
- `AZURE_WEBAPP_PUBLISH_PROFILE` = contenido XML del publish profile

**Variables**
- `AZURE_WEBAPP_NAME` = nombre del Web App (ej: `app-demo-df-123456`)

### 2.3 Ejecutar workflow
- Ve a Actions → workflow `deploy-api` → Run workflow

> El workflow compila y publica la API al Web App.

---

## 3) Validación rápida

### 3.1 Health
Abre:
- `https://<webapp>.azurewebsites.net/health`

Debe responder algo como:
```json
{ "status": "ok", "utc": "..." }
```

### 3.2 Swagger (dev)
En `ASPNETCORE_ENVIRONMENT=Development` verás:
- `https://<webapp>.azurewebsites.net/swagger`

> En producción puedes desactivar swagger (no necesario para el taller).

### 3.3 UI simple
- `https://<webapp>.azurewebsites.net/`

Sube un archivo y luego revisa el listado.

---

## 4) Logs (muy útil para el taller)
Portal → Web App → **Log stream**

Busca logs como:
- `Uploading fileId=... as blob yyyy/MM/dd/...`

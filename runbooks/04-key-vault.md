# 04 - Azure Key Vault

Objetivo del módulo:
- Crear secret `docflow-storage-connectionstring`
- Usar **Key Vault references** desde Web App y Function App
- Validar que la app corre sin exponer secretos en App Settings

---

## 1) Crear secret

En Key Vault → Secrets → Generate/Import:

- Name: `docflow-storage-connectionstring`
- Value: (connection string del Storage Account)

> Si usaste `infra/deploy.sh`, esto ya existe.

---

## 2) Dar permisos al Managed Identity

Para Key Vault references, el Web App / Function App necesita:
- `Get` + `List` secrets

En este repo usamos Access Policies (simple para taller).

Si lo hiciste por script, ya está.
Si es manual:
- Key Vault → Access policies → Create
- Selecciona la identidad del Web App / Function App
- Secret permissions: `Get`, `List`

---

## 3) Configurar Key Vault reference en App Settings

Ejemplo:

`Blob__ConnectionString=@Microsoft.KeyVault(SecretUri=https://<kv>.vault.azure.net/secrets/docflow-storage-connectionstring/<version>)`

En Functions además:
- `AzureWebJobsStorage=@Microsoft.KeyVault(...)`
- `DocFlowStorage=@Microsoft.KeyVault(...)`

---

## 4) Validación

1) Reinicia la Web App / Function App (para forzar refresh de references).
2) Verifica `/health` en la API.
3) Sube un archivo, y confirma que se procesa.

Tip:
- Si falla, revisa:
  - que el SecretUri tenga **versión**
  - que la identidad tenga permisos
  - que no haya typos en el setting

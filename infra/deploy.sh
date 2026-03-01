#!/usr/bin/env bash
set -euo pipefail

# DocFlow Azure Demo - infra bootstrap
# Usage:
#   ./deploy.sh -p <prefix> -l <location> -g <resourceGroup>
#
# Example:
#   ./deploy.sh -p demo -l eastus -g rg-demo-docflow

PREFIX="demo"
LOCATION="eastus"
RG=""
SKU="B1"

while getopts "p:l:g:s:h" opt; do
  case $opt in
    p) PREFIX="$OPTARG" ;;
    l) LOCATION="$OPTARG" ;;
    g) RG="$OPTARG" ;;
    s) SKU="$OPTARG" ;;
    h)
      echo "Usage: $0 -p <prefix> -l <location> -g <resourceGroup> [-s <appServiceSku>]"
      exit 0
      ;;
    *)
      echo "Invalid option"
      exit 1
      ;;
  esac
done

if [[ -z "$RG" ]]; then
  RG="rg-${PREFIX}-docflow"
fi

# sanitize for global resource names
PREFIX_SAFE="$(echo "$PREFIX" | tr '[:upper:]' '[:lower:]' | tr -cd 'a-z0-9' | cut -c1-10)"
SUFFIX="$(date +%s | rev | cut -c1-6 | rev)"

STORAGE="df${PREFIX_SAFE}${SUFFIX}"                         # 2 + up to 10 + 6 = <=18
KV="kv-${PREFIX_SAFE}-df-${SUFFIX}"
PLAN="asp-${PREFIX_SAFE}-df"
WEBAPP="app-${PREFIX_SAFE}-df-${SUFFIX}"
FUNC="func-${PREFIX_SAFE}-df-${SUFFIX}"

echo "== DocFlow Azure Demo =="
echo "RG:       $RG"
echo "Location: $LOCATION"
echo "Storage:  $STORAGE"
echo "KeyVault: $KV"
echo "WebApp:   $WEBAPP"
echo "FuncApp:  $FUNC"
echo

az group create -n "$RG" -l "$LOCATION" 1>/dev/null

echo "== Storage account =="
az storage account create \
  -n "$STORAGE" -g "$RG" -l "$LOCATION" \
  --sku Standard_LRS \
  --allow-blob-public-access false 1>/dev/null

CONN="$(az storage account show-connection-string -n "$STORAGE" -g "$RG" --query connectionString -o tsv)"
echo "Creating containers: uploads, processed"
az storage container create --name uploads   --account-name "$STORAGE" --connection-string "$CONN" 1>/dev/null
az storage container create --name processed --account-name "$STORAGE" --connection-string "$CONN" 1>/dev/null

echo "== Key Vault (Access Policy mode) =="
az keyvault create -n "$KV" -g "$RG" -l "$LOCATION" --enable-rbac-authorization false 1>/dev/null

echo "Creating secret: docflow-storage-connectionstring"
az keyvault secret set --vault-name "$KV" --name "docflow-storage-connectionstring" --value "$CONN" 1>/dev/null
SECRET_URI="$(az keyvault secret show --vault-name "$KV" --name "docflow-storage-connectionstring" --query id -o tsv)"

echo "== App Service (Web App) =="
az appservice plan create -n "$PLAN" -g "$RG" --is-linux --sku "$SKU" 1>/dev/null

# Runtime stack can vary by region/time. We set it after create too.
az webapp create -n "$WEBAPP" -g "$RG" -p "$PLAN" --runtime "DOTNETCORE|10.0" 1>/dev/null || \
az webapp create -n "$WEBAPP" -g "$RG" -p "$PLAN" 1>/dev/null

# Try setting linux FX version (best effort)
az webapp config set -n "$WEBAPP" -g "$RG" --linux-fx-version "DOTNETCORE|10.0" 1>/dev/null || true

WEB_PRINCIPAL_ID="$(az webapp identity assign -n "$WEBAPP" -g "$RG" --query principalId -o tsv)"

echo "Granting Key Vault secret get/list to Web App identity..."
az keyvault set-policy -n "$KV" --object-id "$WEB_PRINCIPAL_ID" --secret-permissions get list 1>/dev/null

echo "Setting Web App appsettings (Key Vault reference + Blob config)..."
az webapp config appsettings set -n "$WEBAPP" -g "$RG" --settings \
  "Blob__Mode=ConnectionString" \
  "Blob__ConnectionString=@Microsoft.KeyVault(SecretUri=${SECRET_URI})" \
  "Blob__UploadsContainer=uploads" \
  "Blob__ProcessedContainer=processed" 1>/dev/null

echo "== Function App (.NET 8 isolated) =="
# Consumption plan creation:
az functionapp create -n "$FUNC" -g "$RG" \
  --consumption-plan-location "$LOCATION" \
  --runtime dotnet-isolated \
  --runtime-version 8 \
  --functions-version 4 \
  --storage-account "$STORAGE" 1>/dev/null

FUNC_PRINCIPAL_ID="$(az functionapp identity assign -n "$FUNC" -g "$RG" --query principalId -o tsv)"

echo "Granting Key Vault secret get/list to Function identity..."
az keyvault set-policy -n "$KV" --object-id "$FUNC_PRINCIPAL_ID" --secret-permissions get list 1>/dev/null

echo "Setting Function App appsettings (Key Vault reference + Blob trigger connection + Blob config)..."
az functionapp config appsettings set -n "$FUNC" -g "$RG" --settings \
  "FUNCTIONS_EXTENSION_VERSION=~4" \
  "FUNCTIONS_WORKER_RUNTIME=dotnet-isolated" \
  "AzureWebJobsStorage=@Microsoft.KeyVault(SecretUri=${SECRET_URI})" \
  "DocFlowStorage=@Microsoft.KeyVault(SecretUri=${SECRET_URI})" \
  "Blob__Mode=ConnectionString" \
  "Blob__ConnectionString=@Microsoft.KeyVault(SecretUri=${SECRET_URI})" \
  "Blob__UploadsContainer=uploads" \
  "Blob__ProcessedContainer=processed" 1>/dev/null

echo "== Optional: grant Storage RBAC for Managed Identity mode =="
SCOPE="$(az storage account show -n "$STORAGE" -g "$RG" --query id -o tsv)"

echo "Assigning 'Storage Blob Data Contributor' to Web App identity..."
az role assignment create \
  --assignee-object-id "$WEB_PRINCIPAL_ID" --assignee-principal-type ServicePrincipal \
  --role "Storage Blob Data Contributor" --scope "$SCOPE" 1>/dev/null || true

echo "Assigning 'Storage Blob Data Contributor' and 'Storage Queue Data Contributor' to Function identity..."
az role assignment create \
  --assignee-object-id "$FUNC_PRINCIPAL_ID" --assignee-principal-type ServicePrincipal \
  --role "Storage Blob Data Contributor" --scope "$SCOPE" 1>/dev/null || true
az role assignment create \
  --assignee-object-id "$FUNC_PRINCIPAL_ID" --assignee-principal-type ServicePrincipal \
  --role "Storage Queue Data Contributor" --scope "$SCOPE" 1>/dev/null || true

echo
echo "== Output =="
echo "Storage Account: $STORAGE"
echo "Key Vault:       $KV"
echo "SecretUri:       $SECRET_URI"
echo "Web App:         https://${WEBAPP}.azurewebsites.net"
echo "Function App:    https://${FUNC}.azurewebsites.net"
echo
echo "Next steps:"
echo "  1) Deploy API -> Web App    (runbooks/01-app-service.md)"
echo "  2) Deploy Functions -> Func (runbooks/03-functions.md)"
echo

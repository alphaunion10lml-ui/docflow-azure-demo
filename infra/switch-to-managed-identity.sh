#!/usr/bin/env bash
set -euo pipefail

# Switch both apps to use Managed Identity for Blob access.
# Note: Blob Triggers require both blob + queue service URIs when using identity-based connections.
# Reference: Azure Functions blob trigger identity-based connection docs.

# Usage:
#   ./switch-to-managed-identity.sh -g <resourceGroup> -w <webAppName> -f <functionAppName> -s <storageAccountName>
#
# Example:
#   ./switch-to-managed-identity.sh -g rg-demo-docflow -w app-demo-df-123456 -f func-demo-df-123456 -s dfdemo123456

RG=""
WEBAPP=""
FUNC=""
STORAGE=""

while getopts "g:w:f:s:h" opt; do
  case $opt in
    g) RG="$OPTARG" ;;
    w) WEBAPP="$OPTARG" ;;
    f) FUNC="$OPTARG" ;;
    s) STORAGE="$OPTARG" ;;
    h)
      echo "Usage: $0 -g <resourceGroup> -w <webAppName> -f <functionAppName> -s <storageAccountName>"
      exit 0
      ;;
    *)
      echo "Invalid option"
      exit 1
      ;;
  esac
done

if [[ -z "$RG" || -z "$WEBAPP" || -z "$FUNC" || -z "$STORAGE" ]]; then
  echo "Missing required args. Run with -h for help."
  exit 1
fi

BLOB_URI="https://${STORAGE}.blob.core.windows.net"
QUEUE_URI="https://${STORAGE}.queue.core.windows.net"

echo "Switching Web App '$WEBAPP' to ManagedIdentity..."
az webapp config appsettings set -n "$WEBAPP" -g "$RG" --settings \
  "Blob__Mode=ManagedIdentity" \
  "Blob__ServiceUri=${BLOB_URI}" 1>/dev/null

echo "Switching Function App '$FUNC' to ManagedIdentity..."
az functionapp config appsettings set -n "$FUNC" -g "$RG" --settings \
  "Blob__Mode=ManagedIdentity" \
  "Blob__ServiceUri=${BLOB_URI}" \
  "DocFlowStorage__blobServiceUri=${BLOB_URI}" \
  "DocFlowStorage__queueServiceUri=${QUEUE_URI}" 1>/dev/null

echo "Done."
echo "Reminder: ensure both managed identities have Storage RBAC:"
echo " - Web App:    Storage Blob Data Contributor"
echo " - Function:   Storage Blob Data Contributor + Storage Queue Data Contributor"

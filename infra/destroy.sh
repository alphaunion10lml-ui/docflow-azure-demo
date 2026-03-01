#!/usr/bin/env bash
set -euo pipefail

# Deletes the resource group (and everything in it).
# Usage: ./destroy.sh -g <resourceGroup>
RG=""
while getopts "g:h" opt; do
  case $opt in
    g) RG="$OPTARG" ;;
    h) echo "Usage: $0 -g <resourceGroup>"; exit 0 ;;
  esac
done
if [[ -z "$RG" ]]; then
  echo "Missing -g <resourceGroup>"
  exit 1
fi
az group delete -n "$RG" --yes --no-wait
echo "Deletion started for RG: $RG"

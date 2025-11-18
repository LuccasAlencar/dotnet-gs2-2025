#!/usr/bin/env bash

###############################################################################
# script-infra-aci.sh
# -----------------------------------------------------------------------------
# Provisiona os recursos essenciais no Azure para o projeto dotnet-gs2-2025:
#   - Resource Group
#   - Azure Container Registry (ACR)
#   - Storage Account + File Share (persistência do MySQL)
#   - Azure Container Instance para o MySQL (imagem oficial mysql:8.0)
#   - Azure Container Instance placeholder para a API (.NET)
#
# Pré-requisitos:
#   - Azure CLI instalado e autenticado (`az login`)
#   - Subscription setada (`az account set --subscription <id>`)
#   - Variáveis sensíveis exportadas antes da execução:
#       export MYSQL_ROOT_PASSWORD="********"
#       export MYSQL_PASSWORD="********"
#
# Uso:
#   ./scripts/script-infra-aci.sh
#   PREFIX=rm556152 LOCATION=brazilsouth ./scripts/script-infra-aci.sh
###############################################################################

set -euo pipefail

# ---------------------------- Variáveis padrão ------------------------------ #
PREFIX=${PREFIX:-rm556152}
LOCATION=${LOCATION:-brazilsouth}
RESOURCE_GROUP=${RESOURCE_GROUP:-rg-${PREFIX}-devops}
ACR_NAME=${ACR_NAME:-acr${PREFIX//-/}devops}
STORAGE_ACCOUNT=${STORAGE_ACCOUNT:-st${PREFIX//-/}aci}
FILE_SHARE=${FILE_SHARE:-fs-mysql-data}
ACI_API_NAME=${ACI_API_NAME:-aci-${PREFIX}-api}
ACI_DB_NAME=${ACI_DB_NAME:-aci-${PREFIX}-mysql}
API_DNS_LABEL=${API_DNS_LABEL:-${ACI_API_NAME}}
MYSQL_DNS_LABEL=${MYSQL_DNS_LABEL:-${ACI_DB_NAME}}
MYSQL_IMAGE=${MYSQL_IMAGE:-mysql:8.0}
ACI_IMAGE_PLACEHOLDER=${ACI_IMAGE_PLACEHOLDER:-mcr.microsoft.com/dotnet/samples:aspnetapp}
MYSQL_DATABASE=${MYSQL_DATABASE:-dotnetgs2}
MYSQL_USER=${MYSQL_USER:-dotnet_api}

# ---------------------- Variáveis sensíveis obrigatórias -------------------- #
: "${MYSQL_ROOT_PASSWORD:?Defina MYSQL_ROOT_PASSWORD antes de executar o script}"
: "${MYSQL_PASSWORD:?Defina MYSQL_PASSWORD antes de executar o script}"

echo "🔐 Subscription atual:"
az account show --query "{name:name, subscriptionId:id}" -o table

# --------------------------- Resource Group --------------------------------- #
echo "📦 Criando/atualizando Resource Group ${RESOURCE_GROUP} (${LOCATION})..."
az group create \
  --name "${RESOURCE_GROUP}" \
  --location "${LOCATION}" \
  --tags project=dotnet-gs2-2025 owner=${PREFIX} >/dev/null

# --------------------------- Azure Container Registry ----------------------- #
if az acr show --name "${ACR_NAME}" --resource-group "${RESOURCE_GROUP}" >/dev/null 2>&1; then
  echo "✅ ACR ${ACR_NAME} já existe."
else
  echo "🚀 Criando ACR ${ACR_NAME}..."
  az acr create \
    --name "${ACR_NAME}" \
    --resource-group "${RESOURCE_GROUP}" \
    --location "${LOCATION}" \
    --sku Standard \
    --admin-enabled true >/dev/null
fi

# --------------------------- Storage Account -------------------------------- #
if az storage account show --name "${STORAGE_ACCOUNT}" --resource-group "${RESOURCE_GROUP}" >/dev/null 2>&1; then
  echo "✅ Storage Account ${STORAGE_ACCOUNT} já existe."
else
  echo "📁 Criando Storage Account ${STORAGE_ACCOUNT}..."
  az storage account create \
    --name "${STORAGE_ACCOUNT}" \
    --resource-group "${RESOURCE_GROUP}" \
    --location "${LOCATION}" \
    --sku Standard_LRS \
    --kind StorageV2 \
    --enable-large-file-share true >/dev/null
fi

STORAGE_KEY=$(az storage account keys list \
  --resource-group "${RESOURCE_GROUP}" \
  --account-name "${STORAGE_ACCOUNT}" \
  --query "[0].value" -o tsv)

if az storage share-rm show --storage-account "${STORAGE_ACCOUNT}" --name "${FILE_SHARE}" >/dev/null 2>&1; then
  echo "✅ File Share ${FILE_SHARE} já existe."
else
  echo "📂 Criando File Share ${FILE_SHARE}..."
  az storage share-rm create \
    --resource-group "${RESOURCE_GROUP}" \
    --storage-account "${STORAGE_ACCOUNT}" \
    --name "${FILE_SHARE}" >/dev/null
fi

# --------------------------- Azure Container Instance - MySQL --------------- #
echo "🗄️  Provisionando Azure Container Instance para MySQL (${MYSQL_IMAGE})..."
az container delete \
  --resource-group "${RESOURCE_GROUP}" \
  --name "${ACI_DB_NAME}" \
  --yes >/dev/null 2>&1 || true

az container create \
  --resource-group "${RESOURCE_GROUP}" \
  --name "${ACI_DB_NAME}" \
  --image "${MYSQL_IMAGE}" \
  --location "${LOCATION}" \
  --dns-name-label "${MYSQL_DNS_LABEL}" \
  --ports 3306 \
  --cpu 2 \
  --memory 4 \
  --restart-policy Always \
  --environment-variables \
    MYSQL_DATABASE="${MYSQL_DATABASE}" \
    MYSQL_USER="${MYSQL_USER}" \
  --secure-environment-variables \
    MYSQL_ROOT_PASSWORD="${MYSQL_ROOT_PASSWORD}" \
    MYSQL_PASSWORD="${MYSQL_PASSWORD}" \
  --azure-file-volume-account-name "${STORAGE_ACCOUNT}" \
  --azure-file-volume-account-key "${STORAGE_KEY}" \
  --azure-file-volume-share-name "${FILE_SHARE}" \
  --azure-file-volume-mount-path "/var/lib/mysql" >/dev/null

MYSQL_FQDN="${MYSQL_DNS_LABEL}.${LOCATION}.azurecontainer.io"
echo "✅ MySQL disponível em ${MYSQL_FQDN}:3306"

# --------------------------- Azure Container Instance - API ----------------- #
echo "🌐 Provisionando container placeholder para a API (.NET)..."
az container delete \
  --resource-group "${RESOURCE_GROUP}" \
  --name "${ACI_API_NAME}" \
  --yes >/dev/null 2>&1 || true

az container create \
  --resource-group "${RESOURCE_GROUP}" \
  --name "${ACI_API_NAME}" \
  --image "${ACI_IMAGE_PLACEHOLDER}" \
  --location "${LOCATION}" \
  --dns-name-label "${API_DNS_LABEL}" \
  --ports 8080 \
  --cpu 1 \
  --memory 2 \
  --restart-policy Always \
  --environment-variables \
    ASPNETCORE_URLS="http://+:8080" \
    DB_HOST="${MYSQL_FQDN}" \
    DB_NAME="${MYSQL_DATABASE}" \
    DB_USER="${MYSQL_USER}" \
  --secure-environment-variables \
    DB_PASSWORD="${MYSQL_PASSWORD}"

API_FQDN="${API_DNS_LABEL}.${LOCATION}.azurecontainer.io"

cat <<EOF
🎉 Provisionamento concluído!

Resumo:
  Resource Group ..........: ${RESOURCE_GROUP}
  Azure Container Registry : ${ACR_NAME}
  Storage Account .........: ${STORAGE_ACCOUNT}
  File Share ..............: ${FILE_SHARE}
  MySQL ACI ...............: ${ACI_DB_NAME} (${MYSQL_FQDN})
  API ACI (placeholder) ...: ${ACI_API_NAME} (${API_FQDN})

Próximos passos:
  1. Configure o Service Connection no Azure DevOps com o Resource Group acima.
  2. Atualize o Variable Group com as mesmas credenciais usadas aqui.
  3. Execute a pipeline (azure-pipelines.yml) para publicar a imagem e redeployar a API.
EOF


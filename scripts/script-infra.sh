#!/bin/bash

# Script de Provisionamento de Infraestrutura na Azure
# Provisiona: Resource Group, Container Registry, Container Instance, MySQL e Storage

set -e  # Exit on error

# Variáveis de configuração


echo "════════════════════════════════════════════════════════════"
echo "🚀 Iniciando Provisionamento de Infraestrutura Azure"
echo "════════════════════════════════════════════════════════════"

# 1. Selecionar assinatura correta
echo ""
echo "1️⃣  Selecionando assinatura Azure..."
az account set --subscription $SUBSCRIPTION_ID

# 2. Criar Resource Group
echo ""
echo "2️⃣  Criando Resource Group: $RESOURCE_GROUP..."
if az group show --name $RESOURCE_GROUP &> /dev/null; then
  echo "⚠️  Resource Group já existe"
else
  az group create \
    --name $RESOURCE_GROUP \
    --location $LOCATION \
    --output json
  echo "✅ Resource Group criado"
fi

# 3. Criar Container Registry (ACR)
echo ""
echo "3️⃣  Criando Azure Container Registry: $ACR_NAME..."
if az acr show --name $ACR_NAME --resource-group $RESOURCE_GROUP &> /dev/null; then
  echo "⚠️  ACR já existe"
else
  az acr create \
    --resource-group $RESOURCE_GROUP \
    --name $ACR_NAME \
    --sku Basic \
    --admin-enabled true \
    --output json
  echo "✅ ACR criado"
fi

# Aguarda o provisionamento do ACR
MAX_RETRIES=12
RETRY=0
while true; do
  EXISTS=$(az acr show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query "provisioningState" -o tsv 2>/dev/null)
  if [ "$EXISTS" == "Succeeded" ]; then
    break
  fi
  RETRY=$((RETRY+1))
  if [ $RETRY -ge $MAX_RETRIES ]; then
    echo "❌ Timeout esperando o ACR ser provisionado."
    exit 1
  fi
  echo "⏳ Aguardando ACR... Tentativa $RETRY de $MAX_RETRIES."
  sleep 10
done

# Obter credentials do ACR
echo ""
echo "4️⃣  Obtendo credenciais do ACR..."
ACR_USERNAME=$(az acr credential show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query username -o tsv)
ACR_PASSWORD=$(az acr credential show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query passwords[0].value -o tsv)

echo "Registry URL: $REGISTRY_URL"
echo "Registry Username: $ACR_USERNAME"

# 5. Criar Storage Account para artefatos
echo ""
echo "5️⃣  Criando Storage Account: $STORAGE_ACCOUNT..."
if az storage account show --name $STORAGE_ACCOUNT --resource-group $RESOURCE_GROUP &> /dev/null; then
  echo "⚠️  Storage Account já existe"
else
  az storage account create \
    --name $STORAGE_ACCOUNT \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --sku Standard_LRS \
    --kind StorageV2 \
    --output json
  echo "✅ Storage Account criado"
fi

# Aguarda o provisionamento do Storage Account
sleep 10

# Criar container no storage
echo ""
echo "6️⃣  Criando blob container para artefatos..."
STORAGE_ACCOUNT_KEY=$(az storage account keys list --resource-group $RESOURCE_GROUP --account-name $STORAGE_ACCOUNT --query [0].value -o tsv)

az storage container create \
  --name artifacts \
  --account-name $STORAGE_ACCOUNT \
  --account-key $STORAGE_ACCOUNT_KEY \
  --output json

# 7. Criar Azure Database for MySQL
echo ""
echo "7️⃣  Criando Azure Database for MySQL: $MYSQL_SERVER..."
az mysql flexible-server create \
  --name $MYSQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --admin-user $MYSQL_ADMIN_USER \
  --admin-password $MYSQL_ADMIN_PASSWORD \
  --database-name $MYSQL_DATABASE \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 32 \
  --high-availability Disabled \
  --output json

# 8. Configurar firewall do MySQL para permitir Azure Services
echo ""
echo "8️⃣  Configurando firewall do MySQL..."
az mysql flexible-server firewall-rule create \
  --name "AllowAzureServices" \
  --server-name $MYSQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --start-ip-address "0.0.0.0" \
  --end-ip-address "0.0.0.0" \
  --output json

# 9. Criar arquivo de saída com valores importantes
echo ""
echo "9️⃣  Salvando configurações em arquivo..."
cat > infrastructure-output.env <<EOF
# Variáveis de Infraestrutura Provisionadas
RESOURCE_GROUP=$RESOURCE_GROUP
LOCATION=$LOCATION
ACR_NAME=$ACR_NAME
ACR_USERNAME=$ACR_USERNAME
ACR_PASSWORD=$ACR_PASSWORD
REGISTRY_URL=$REGISTRY_URL
ACI_NAME=$ACI_NAME
STORAGE_ACCOUNT=$STORAGE_ACCOUNT
STORAGE_ACCOUNT_KEY=$STORAGE_ACCOUNT_KEY
MYSQL_SERVER=$MYSQL_SERVER
MYSQL_DATABASE=$MYSQL_DATABASE
MYSQL_ADMIN_USER=$MYSQL_ADMIN_USER
MYSQL_ADMIN_PASSWORD=$MYSQL_ADMIN_PASSWORD
MYSQL_HOST=${MYSQL_SERVER}.mysql.database.azure.com
MYSQL_PORT=3306
APP_NAME=$APP_NAME
IMAGE_NAME=$IMAGE_NAME
EOF

echo ""
echo "✅ Infraestrutura provisionada com sucesso!"
echo "════════════════════════════════════════════════════════════"
echo ""
echo "📝 Configurações salvas em: infrastructure-output.env"
echo ""
echo "Próximos passos:"
echo "1. Configurar as variáveis de ambiente no Azure DevOps"
echo "2. Fazer o build da imagem Docker"
echo "3. Deploy da aplicação no ACI"
echo ""
echo "════════════════════════════════════════════════════════════"

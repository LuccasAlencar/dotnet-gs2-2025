# Users API Buscadora de Vagas - .NET 8

API RESTful para busca de vagas de emprego usando Adzuna API, com gerenciamento de usuários e análise de currículo. Desenvolvida em .NET 8 com Oracle Database (compatível com MySQL 8 em container), seguindo as melhores práticas de desenvolvimento e arquitetura de software.

## 📋 Índice

- [Características](#características)
- [Tecnologias](#tecnologias)
- [Arquitetura](#arquitetura)
- [DevOps & Cloud](#-devops--cloud)
- [Pré-requisitos](#pré-requisitos)
- [Configuração](#configuração)
- [Executando o Projeto](#executando-o-projeto)
- [Versionamento da API](#versionamento-da-api)
- [Endpoints](#endpoints)
- [Health Checks](#health-checks)
- [Logging e Observabilidade](#logging-e-observabilidade)
- [HATEOAS](#hateoas)

## 🚀 Características

### 1. Boas Práticas REST
- ✅ **Paginação**: Suporte completo para paginação de resultados
- ✅ **HATEOAS**: Hypermedia As The Engine Of Application State
- ✅ **Status Codes**: Uso adequado de códigos HTTP (200, 201, 204, 400, 404, 409)
- ✅ **Verbos HTTP**: GET, POST, PUT, DELETE corretamente implementados
- ✅ **Validação de Dados**: Data Annotations e validações personalizadas

### 2. Monitoramento e Observabilidade
- ✅ **Health Checks**: Endpoints de verificação de saúde da aplicação
- ✅ **Logging**: Serilog com logs estruturados em Console e Arquivo
- ✅ **Tracing**: OpenTelemetry para rastreamento distribuído

### 3. Versionamento da API
- ✅ **Múltiplas Versões**: Suporte para `/api/v1` e `/api/v2`
- ✅ **Versionamento por URL**: Rotas versionadas
- ✅ **Versionamento por Header**: Suporte via `X-API-Version`
- ✅ **Versionamento por Query String**: Suporte via `?api-version=1.0`

### 4. Integração e Persistência
- ✅ **Oracle / MySQL Database**: Integração completa com Oracle (on-prem) e MySQL 8 no Azure Container Instance
- ✅ **Entity Framework Core**: ORM moderno e eficiente
- ✅ **Repository Pattern**: Separação de responsabilidades
- ✅ **BCrypt**: Hash seguro de senhas
- ✅ **Adzuna API**: Busca de vagas de emprego em tempo real
- ✅ **Variáveis de Ambiente**: Suporte a arquivo .env para credenciais

### 5. Frontend
- ✅ **Interface Web**: HTML/CSS/JavaScript responsivo
- ✅ **Upload de Currículo**: Extração de habilidades via Hugging Face
- ✅ **Sugestão Automática**: Localização e palavras-chave preenchidas pelo currículo
- ✅ **Busca de Vagas**: Disparo automático após upload (editável pelo usuário)
- ✅ **Listagem de Resultados**: Cards informativos com link para vaga

## 🛠️ Tecnologias

- **.NET 8**: Framework principal
- **ASP.NET Core**: Web API
- **Oracle Database**: Banco de dados relacional
- **MySQL 8 (ACI)**: Alternativa containerizada para produção
- **Entity Framework Core**: ORM
- **Serilog**: Logging estruturado
- **OpenTelemetry**: Observabilidade e tracing
- **Swagger/OpenAPI**: Documentação interativa
- **Asp.Versioning**: Versionamento de API
- **Adzuna API**: API externa para busca de vagas
- **DotNetEnv**: Gerenciamento de variáveis de ambiente
- **HTML/CSS/JavaScript**: Frontend básico e responsivo

## 🏗️ Arquitetura

```
dotnet-gs2-2025/
├── Controllers/
│   ├── V1/
│   │   ├── JobsController.cs     # Busca de vagas
│   │   ├── ResumesController.cs  # Processamento de currículo
│   │   └── UsersController.cs    # API versão 1
│   ├── V2/
│   │   └── UsersController.cs    # API versão 2
│   └── HealthController.cs        # Health check
├── Configuration/
│   └── HuggingFaceOptions.cs     # Configurações do modelo IA
├── Data/
│   └── ApplicationDbContext.cs    # Contexto do EF Core
├── Models/
│   ├── HuggingFaceEntity.cs       # Entidades retornadas pela IA
│   ├── User.cs                    # Entidade User
│   └── DTOs/
│       ├── JobDto.cs              # DTO de vagas
│       ├── UserCreateDto.cs       # DTO para criação
│       ├── UserUpdateDto.cs       # DTO para atualização
│       ├── UserResponseDto.cs     # DTO para resposta
│       ├── PagedResponse.cs       # DTO para paginação
│       ├── ResumeUploadRequestDto.cs # DTO upload de currículo
│       ├── SkillExtractionResponseDto.cs # DTO resposta Hugging Face
│       ├── SkillExtractionResult.cs # Resultado interno de extração
│       └── Link.cs                # DTO para HATEOAS
│   └── ResumeExtraction.cs        # Entidades consolidadas do currículo
├── Repositories/
│   ├── IUserRepository.cs         # Interface do repositório
│   └── UserRepository.cs          # Implementação do repositório
├── Services/
│   ├── IAdzunaService.cs          # Interface de vagas
│   ├── IHuggingFaceService.cs     # Interface IA de habilidades
│   ├── IResumeService.cs          # Interface processamento currículo
│   ├── IUserService.cs            # Interface do serviço
│   ├── AdzunaService.cs           # Integração com Adzuna
│   ├── HuggingFaceService.cs      # Integração com Hugging Face
│   ├── PdfTextExtractor.cs        # Leitura de texto em PDFs
│   ├── ResumeService.cs           # Orquestra extração de habilidades
│   └── UserService.cs             # Lógica de usuários
├── logs/                          # Logs da aplicação
├── appsettings.json               # Configurações
└── Program.cs                     # Configuração da aplicação
```

### Desenho macro (Mermaid)

```mermaid
flowchart LR
    Dev[(GitHub<br/>dotnet-gs2-2025)]
    Boards[[Azure Boards<br/>Work Items]]
    Pipelines[[Azure Pipelines<br/>CI/CD]]
    ACR[(Azure Container Registry)]
    ACIApp[[Azure Container Instance<br/>API .NET 8]]
    ACIDb[[Azure Container Instance<br/>MySQL 8.0]]
    Users((Usuários / Frontend))

    Dev -->|commit / PR| Pipelines
    Boards -->|link| Pipelines
    Pipelines -->|artefato + testes| Pipelines
    Pipelines -->|imagem| ACR
    ACR -->|pull| Pipelines
    Pipelines -->|release| ACIApp
    ACIApp -->|TCP 3306| ACIDb
    Users -->|HTTPS 8080| ACIApp
```

## ☁️ DevOps & Cloud

- **Provisionamento**: `scripts/script-infra-aci.sh` cria Resource Group, ACR, Storage + File Share, ACI para MySQL (imagem oficial `mysql:8.0`) e um container placeholder da API. Execute após exportar as variáveis sensíveis:

  ```bash
  export MYSQL_ROOT_PASSWORD='SenhaRaizForte!'
  export MYSQL_PASSWORD='SenhaAplicacao!'
  ./scripts/script-infra-aci.sh
  ```

  Parâmetros como `PREFIX`, `LOCATION`, `ACR_NAME` podem ser sobrescritos via variáveis de ambiente.

- **Banco**: `scripts/script-bd.sql` provisiona o schema (tabelas `USERS`, `RESUMES`, `JOB_SEARCH_AUDIT`) no MySQL/ACI. O script é executado automaticamente pela pipeline de release antes do primeiro deploy, mas pode ser aplicado manualmente:

  ```bash
  mysql -h <fqdn-mysql> -u dotnet_api -p < scripts/script-bd.sql
  ```

- **Dockerfiles**: `dockerfiles/Dockerfile.api` gera a imagem multi-stage (SDK + ASP.NET runtime) publicada no ACR e utilizada pelo Azure Container Instance do ambiente.

- **Azure Boards + Branch Policy**:
  - Crie a tarefa inicial no Boards e use o padrão `feature/RM556152-<ID>` para o branch.
  - Vincule commits usando `git commit -m "Implementa build #123"`.
  - Proteja a branch `main` exigindo: _Reviewer obrigatório_, _Work Item linkado_, _Default reviewer `RM 556152`_, _Merge somente via PR_. Você pode manter "Permitir auto-aprovação" habilitado para simular o cenário do curso.

- **Pipelines (azure-pipelines.yml)**:
  1. **Build** (executa após merge em `main`): restaura, builda, roda testes (`dotnet test`), publica resultados TRX e o artefato `drop`.
  2. **ContainerImage**: builda e envia `dockerfiles/Dockerfile.api` para o ACR (`<acr>.azurecr.io/dotnet-gs2-api:<buildId>` + `latest`).
  3. **Release**: após a imagem, o AzureCLI recria o container da API no ACI usando os recursos provisionados pelo script. Isso garante deploy automático assim que um novo artefato é gerado.

- **Variáveis protegidas**: crie um Variable Group `dotnet-gs2-secrets` (linkado ao pipeline) contendo:

  | Variável | Exemplo |
  |----------|---------|
  | `ADZUNA_APP_ID` | `xxxxxxxx` |
  | `ADZUNA_APP_KEY` | (secreto) |
  | `HUGGINGFACE__TOKEN` | (secreto) |
  | `MYSQL_DATABASE` | `dotnetgs2` |
  | `MYSQL_USER` | `dotnet_api` |
  | `MYSQL_PASSWORD` | (secreto) |

- **Service Connection**: Atualize o valor da variável `azureSubscription` no YAML para o nome do Service Connection que tem acesso ao Resource Group criado pelo script.

- **Release automático**: não há stage manual; qualquer Build bem-sucedido em `main` dispara a Release e recria o container com a nova versão.

### CRUD exposto em JSON

```json
{
  "create": { "method": "POST", "path": "/api/v1/users" },
  "read":   { "method": "GET", "path": "/api/v1/users/{id}" },
  "update": { "method": "PUT", "path": "/api/v1/users/{id}" },
  "delete": { "method": "DELETE", "path": "/api/v1/users/{id}" },
  "list":   { "method": "GET", "path": "/api/v1/users?page=1&pageSize=10" }
}
```


## 📦 Pré-requisitos

- **.NET 8 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Oracle Database**: Versão 11g ou superior (se optar por Oracle)
- **Oracle Client**: Oracle Data Provider for .NET
- **MySQL 8**: Rodar via Docker/ACI (para o deploy em nuvem)
- **Azure CLI 2.63+**: Necessário para os scripts de provisionamento

## ⚙️ Configuração

### 1. Clone o repositório

```bash
git clone <url-do-repositorio>
cd dotnet-gs2-2025
```

### 2. Configure as variáveis de ambiente

Crie um arquivo `.env` na raiz do projeto com suas credenciais:

```env
# Database Provider
DB_PROVIDER=mysql

# Adzuna API Credentials
ADZUNA_APP_ID=seu_app_id_aqui
ADZUNA_APP_KEY=seu_app_key_aqui

# Hugging Face
HUGGINGFACE__TOKEN=seu_token_hugging_face

# MySQL (Azure Container Instance)
MYSQL_HOST=aci-rm556152-mysql.brazilsouth.azurecontainer.io
MYSQL_PORT=3306
MYSQL_DATABASE=dotnetgs2
MYSQL_USER=dotnet_api
MYSQL_PASSWORD=sua_senha_mysql

# Oracle Database Credentials (opcional)
ORACLE_USER_ID=seu_usuario
ORACLE_PASSWORD=sua_senha
ORACLE_DATA_SOURCE=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=host)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=orcl)))
```

Defina `DB_PROVIDER=oracle` se preferir manter a integração com Oracle. Para o ambiente em nuvem (ACI + MySQL), mantenha `DB_PROVIDER=mysql` e ajuste os hosts/FQDN gerados pelo script de infraestrutura.

**Obtenha suas credenciais Adzuna em**: https://developer.adzuna.com/
**Token da API Hugging Face**: https://huggingface.co/settings/tokens

### 3. Certifique-se que a tabela existe no banco

Se estiver usando **Oracle**, garanta que a tabela `USERS` existe no schema:

```sql
CREATE TABLE users (
    id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR2(100) NOT NULL,
    email VARCHAR2(150) UNIQUE NOT NULL,
    password VARCHAR2(255) NOT NULL,
    phone VARCHAR2(20),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

Se estiver usando **MySQL/ACI**, basta executar `scripts/script-bd.sql` (manual ou via pipeline) para criar as mesmas tabelas.

### 4. Restaure os pacotes

```bash
dotnet restore
```

## Executando o Projeto

### Modo Desenvolvimento

```bash
dotnet run
```

A aplicação estará disponível em:
- **HTTP**: http://localhost:5000
- **HTTPS**: https://localhost:5001
- **Swagger UI**: http://localhost:5000 (raiz)

### 5. Abra o Frontend

Abra o arquivo `frontend/index.html` no seu navegador ou use um servidor web local:

```bash
# Se tiver Python instalado
cd frontend
python -m http.server 8080
```

Depois acesse: `http://localhost:8080`

### Build para Produção

```bash
dotnet build -c Release
dotnet publish -c Release -o ./publish
```

## 🔄 Versionamento da API

A API suporta múltiplas formas de versionamento:

### 1. Via URL (Recomendado)
```
GET /api/v1/users
GET /api/v2/users
```

### 2. Via Header
```
GET /api/users
X-API-Version: 1.0
```

### 3. Via Query String
```
GET /api/users?api-version=1.0
GET /api/users?api-version=2.0
```

## 📍 Endpoints

### Versão 1 (v1)

#### GET /api/v1/users
Retorna lista paginada de usuários.

**Query Parameters:**
- `page` (int, default: 1): Número da página
- `pageSize` (int, default: 10, max: 100): Tamanho da página

**Resposta (200 OK):**
```json
{
  "page": 1,
  "pageSize": 10,
  "totalItems": 100,
  "totalPages": 10,
  "data": [
    {
      "id": 1,
      "name": "João Silva",
      "email": "joao@email.com",
      "phone": "(11) 98765-4321",
      "createdAt": "2024-01-01T10:00:00Z",
      "updatedAt": "2024-01-01T10:00:00Z",
      "links": [
        {
          "href": "http://localhost:5000/api/v1/users/1",
          "rel": "self",
          "method": "GET"
        },
        {
          "href": "http://localhost:5000/api/v1/users/1",
          "rel": "update",
          "method": "PUT"
        },
        {
          "href": "http://localhost:5000/api/v1/users/1",
          "rel": "delete",
          "method": "DELETE"
        }
      ]
    }
  ],
  "links": [
    {
      "href": "http://localhost:5000/api/v1/users?page=1&pageSize=10",
      "rel": "self",
      "method": "GET"
    },
    {
      "href": "http://localhost:5000/api/v1/users?page=2&pageSize=10",
      "rel": "next",
      "method": "GET"
    }
  ]
}
```

#### GET /api/v1/users/{id}
Retorna um usuário específico.

**Respostas:**
- `200 OK`: Usuário encontrado
- `404 Not Found`: Usuário não existe

#### POST /api/v1/users
Cria um novo usuário.

**Body:**
```json
{
  "name": "João Silva",
  "email": "joao@email.com",
  "password": "senha123",
  "phone": "(11) 98765-4321"
}
```

**Respostas:**
- `201 Created`: Usuário criado com sucesso
- `400 Bad Request`: Dados inválidos
- `409 Conflict`: Email já cadastrado

#### PUT /api/v1/users/{id}
Atualiza um usuário existente.

**Body (todos os campos são opcionais):**
```json
{
  "name": "João Silva Atualizado",
  "email": "joao.novo@email.com",
  "password": "novaSenha123",
  "phone": "(11) 98765-4321"
}
```

**Respostas:**
- `200 OK`: Usuário atualizado
- `400 Bad Request`: Dados inválidos
- `404 Not Found`: Usuário não existe
- `409 Conflict`: Email já cadastrado

#### DELETE /api/v1/users/{id}
Remove um usuário.

**Respostas:**
- `204 No Content`: Usuário removido
- `404 Not Found`: Usuário não existe

#### POST /api/v1/resumes/skills
Extrai habilidades de um currículo em PDF usando a IA Hugging Face.

**Form-Data:**
- `file` (arquivo, obrigatório): Currículo em formato PDF (máx. 5MB)

**Resposta (200 OK):**
```json
{
  "skills": ["Java", "Spring", "SQL"],
  "totalSkills": 3,
  "textLength": 12345,
  "locations": ["São Paulo", "Brasil"],
  "suggestedLocation": "São Paulo",
  "metadata": {
    "fileName": "curriculo.pdf",
    "fileSizeBytes": 345678
  },
  "links": [
    {
      "href": "http://localhost:5000/api/v1/resumes/skills",
      "rel": "self",
      "method": "POST"
    },
    {
      "href": "http://localhost:5000/api/v1/jobs/search",
      "rel": "jobs-search",
      "method": "POST"
    }
  ]
}
```

### Versão 2 (v2)

A versão 2 possui os mesmos endpoints com melhorias:
- **Page Size padrão**: 20 (ao invés de 10)
- **Headers adicionais**: `X-API-Version`, `X-Total-Count`, `X-Total-Pages`
- **Respostas de erro melhoradas**: Incluem `version` e `timestamp`

## 🏥 Health Checks

A API possui três endpoints de health check:

### 1. Health Check Completo
```
GET /health
```

Verifica todos os componentes incluindo banco de dados.

**Resposta (200 OK):**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "oracle-database": {
      "status": "Healthy",
      "duration": "00:00:00.1234567",
      "tags": ["db", "oracle", "database"]
    }
  }
}
```

### 2. Readiness Check
```
GET /health/ready
```

Verifica se a aplicação está pronta para receber tráfego.

### 3. Liveness Check
```
GET /health/live
```

Verifica se a aplicação está viva.

## 📊 Logging e Observabilidade

### Logging (Serilog)

Os logs são gravados em:
- **Console**: Logs formatados para desenvolvimento
- **Arquivo**: `logs/api-{Date}.log` (rotação diária)

**Níveis de Log:**
- Information: Eventos normais da aplicação
- Warning: Situações anormais mas recuperáveis
- Error: Erros que precisam atenção
- Fatal: Erros críticos que param a aplicação

### Tracing (OpenTelemetry)

A aplicação possui instrumentação para:
- **ASP.NET Core**: Requisições HTTP
- **HTTP Client**: Chamadas externas
- **Console Exporter**: Traces exibidos no console

## 🔗 HATEOAS

Todos os endpoints retornam links HATEOAS para navegação pela API.

**Tipos de Links:**
- `self`: Link para o próprio recurso
- `update`: Link para atualizar o recurso
- `delete`: Link para deletar o recurso
- `all-users`: Link para listar todos os usuários
- `next`: Próxima página (paginação)
- `previous`: Página anterior (paginação)
- `first`: Primeira página (paginação)
- `last`: Última página (paginação)

## 📝 Exemplos de Uso

### cURL

```bash
# Listar usuários
curl -X GET "http://localhost:5000/api/v1/users?page=1&pageSize=10"

# Obter usuário específico
curl -X GET "http://localhost:5000/api/v1/users/1"

# Criar usuário
curl -X POST "http://localhost:5000/api/v1/users" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "João Silva",
    "email": "joao@email.com",
    "password": "senha123",
    "phone": "(11) 98765-4321"
  }'

# Atualizar usuário
curl -X PUT "http://localhost:5000/api/v1/users/1" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "João Silva Atualizado"
  }'

# Deletar usuário
curl -X DELETE "http://localhost:5000/api/v1/users/1"

# Health Check
curl -X GET "http://localhost:5000/health"
```

### PowerShell

```powershell
# Listar usuários
Invoke-RestMethod -Uri "http://localhost:5000/api/v1/users?page=1&pageSize=10" -Method Get

# Criar usuário
$body = @{
    name = "João Silva"
    email = "joao@email.com"
    password = "senha123"
    phone = "(11) 98765-4321"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/v1/users" -Method Post -Body $body -ContentType "application/json"
```

## 🔒 Segurança

✅ **Hash de Senhas com BCrypt**
- Implementado BCrypt para hash seguro de senhas
- Work factor configurado em 12 (bom equilíbrio entre segurança e performance)
- Salt automático único para cada senha
- Padrão da indústria para armazenamento seguro de senhas

## 📄 Licença

Este projeto é de código aberto para fins educacionais.




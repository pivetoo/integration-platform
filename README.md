# Integration Platform

Integration Platform é uma plataforma de orquestração de integrações criada para modelar, executar, monitorar e depurar fluxos entre sistemas internos e externos.

O projeto faz parte do ecossistema Mainstay/Archon e foi desenvolvido com foco em arquitetura limpa, pipelines reutilizáveis, observabilidade operacional e autenticação centralizada via Identity Management.

## Visão Geral

Em integrações corporativas, normalmente é necessário conectar APIs, bancos de dados, scripts e regras de transformação em um fluxo rastreável. A Integration Platform resolve esse cenário oferecendo:

- cadastro de integrações e conectores;
- definição de atributos configuráveis por integração;
- pipelines com etapas ordenadas;
- execução de chamadas HTTP, funções JavaScript e scripts SQL;
- execução manual, enfileirada ou em modo debug;
- logs detalhados por etapa;
- dashboard operacional;
- mapeamento de referências entre IDs internos e externos;
- rotinas agendadas de pipelines;
- autenticação e autorização integradas ao Identity Management.

## Principais Features

- **Pipeline Engine**
  Motor de execução que carrega o conector, monta o contexto, executa etapas ativas em ordem e registra logs de cada passo.

- **Etapas Reutilizáveis**
  A plataforma permite compor pipelines usando chamadas HTTP, funções JavaScript e scripts SQL cadastrados previamente.

- **Debug de Pipeline**
  Execução passo a passo com sessão temporária em memória, permitindo inspecionar variáveis, payload, atributos e resultado intermediário.

- **Fila de Processamento**
  Pipelines podem ser enfileirados para processamento posterior, mantendo histórico e status da operação.

- **Observabilidade**
  Cada execução gera registros de status, logs, erros e resultados, facilitando diagnóstico e auditoria.

- **Referências Externas**
  O módulo de referências permite mapear identificadores internos e externos, comum em integrações com ERPs, CRMs e APIs terceiras.

- **Identity Management**
  Quando configurada, a API usa o Identity Management como provedor de identidade, valida tokens via Archon Framework e sincroniza recursos protegidos.

## Arquitetura

O backend segue uma organização em camadas:

```text
IntegrationPlatform/
  IntegrationPlatform.Api/             Host HTTP, controllers, CORS, auth e OpenAPI
  IntegrationPlatform.Application/     Requests, responses, contratos e interfaces
  IntegrationPlatform.Domain/          Entidades e enums do domínio
  IntegrationPlatform.Infrastructure/  EF Core, migrations, services e motor de execução
  IntegrationPlatform.Testing/         Projeto de testes
  IntegrationPlatform.Web/             SPA administrativa e operacional
```

## Modelo de Domínio

As principais entidades do sistema são:

- `IntegrationCategory`: organiza integrações por categoria.
- `Integration`: representa uma integração lógica.
- `IntegrationAttribute`: define campos configuráveis da integração.
- `Connector`: instância concreta usada na execução.
- `ConnectorAttributeValue`: valores preenchidos para um conector.
- `ApiCall`: definição reutilizável de chamada HTTP.
- `JavaScriptFunction`: função executável dentro do pipeline.
- `DatabaseConnection`: conexão com banco externo.
- `DatabaseScript`: script SQL reutilizável.
- `Pipeline`: fluxo executável da integração.
- `PipelineStep`: etapa ordenada do pipeline.
- `ProcessingQueue`: fila persistida de processamento.
- `Execution`: registro de execução.
- `ExecutionLog`: log detalhado da execução.
- `PipelineRoutine`: rotina agendada para execução de pipeline.

## Motor de Execução

O núcleo de execução fica principalmente em:

- `ExecutionEngineService`;
- `StepExecutorService`;
- `QueueProcessorService`.

Fluxo básico:

1. Carrega conector, pipeline e etapas.
2. Monta o contexto da execução com payload, atributos e variáveis.
3. Executa as etapas ativas em ordem.
4. Interpola templates com dados do contexto.
5. Registra logs por etapa.
6. Extrai variáveis do resultado para uso nas próximas etapas.
7. Finaliza a execução com status de sucesso, erro ou parcial.

O comportamento em caso de erro é controlado por `PipelineStep.ErrorAction`:

- `Stop`: interrompe o pipeline.
- `Continue`: registra a falha e segue para a próxima etapa.

## Tipos de Etapa

### HTTP Request

Executa uma `ApiCall` com:

- método HTTP;
- URL;
- headers com template;
- body com template.

Antes da requisição, o executor interpola dados como payload, atributos, variáveis e resultados anteriores.

### JavaScript Function

Executa funções JavaScript usando Jint, com:

- timeout;
- limite de memória;
- limite de recursão.

O script pode acessar:

- `variables`;
- `payload`;
- `attributes`;
- `result`.

### Execute Script

Executa scripts SQL em bancos externos configurados no módulo de conexões.

Suporte atual:

- PostgreSQL;
- SQL Server.

Proteções implementadas:

- bloqueio de `UPDATE` sem `WHERE`;
- bloqueio de `DELETE` sem `WHERE`.

## Modos de Execução

A plataforma suporta:

- execução manual de pipeline;
- execução por identificador lógico da integração e do pipeline;
- enfileiramento para processamento posterior;
- processamento de item da fila;
- debug passo a passo.

Endpoints relevantes:

- `POST /api/executions/execute`
- `POST /api/executions/execute-by-identifier/{integrationIdentifier}/{pipelineIdentifier}`
- `POST /api/executions/debug/start`
- `POST /api/executions/debug/next-step`
- `POST /api/executions/debug/finish`
- `POST /api/processingqueues/enqueue`
- `POST /api/processingqueues/{processingQueueId}/process`

## Módulos da Interface

O frontend possui módulos para:

- dashboard;
- categorias de integração;
- integrações;
- atributos de integração;
- conectores;
- pipelines;
- etapas de pipeline;
- chamadas de API;
- funções JavaScript;
- conexões de banco;
- scripts SQL;
- execuções;
- fila de processamento;
- referências;
- rotinas de automação;
- debugger visual de pipelines.

## Dashboard

O dashboard consolida indicadores operacionais como:

- integrações ativas;
- conectores ativos;
- pipelines ativos;
- execuções do dia;
- erros do dia;
- taxa de sucesso;
- execuções mensais;
- execuções recentes.

## Stack

Backend:

- .NET
- ASP.NET Core
- Entity Framework Core
- FluentMigrator via Archon Infrastructure
- Jint
- Npgsql
- Microsoft.Data.SqlClient
- Scalar/OpenAPI
- Archon Framework

Frontend:

- React
- TypeScript
- Vite
- React Router
- Tailwind CSS
- Archon UI

Infra:

- PostgreSQL
- Docker Compose para deploy
- NGINX/reverse proxy em produção
- GitHub Actions para pipeline de deploy

## Autenticação e Autorização

A API pode operar integrada ao Identity Management quando a configuração `IdentityManagement:Authority` está presente.

Nesse modo:

- o frontend inicia login OIDC com o `client_id` configurado;
- o Identity Management emite tokens para o contrato selecionado;
- a API valida o access token via Archon Framework;
- recursos protegidos podem ser sincronizados automaticamente com o Identity Management;
- permissões são aplicadas por role e contrato.

## Configuração

Exemplo simplificado de configuração local da API:

```json
{
  "TenantDatabases": {
    "default": {
      "CompanyName": "Integration Platform",
      "TenantId": "00000000-0000-0000-0000-000000000000",
      "ConnectionString": "Host=localhost;Port=5432;Database=integrationplatform;Username=postgres;Password=postgres;",
      "DatabaseType": "PostgreSql",
      "Schema": "public",
      "IntegrationSecret": "local-development-secret"
    }
  },
  "IdentityManagement": {
    "Authority": "https://auth.localhost"
  },
  "Jwt": {
    "Issuer": "identity-management",
    "Audience": "integration-platform"
  },
  "RunMigrations": true
}
```

Variáveis comuns do frontend:

```env
VITE_API_BASE_URL=https://localhost:7078/api
VITE_IDENTITY_MANAGEMENT_URL=https://auth.localhost
VITE_OIDC_CLIENT_ID=integration-platform-dev
```

Não versionar credenciais reais, connection strings produtivas ou secrets.

## Como Rodar Localmente

Backend:

```bash
cd IntegrationPlatform
dotnet restore
dotnet run --project IntegrationPlatform.Api/IntegrationPlatform.Api.csproj
```

Frontend:

```bash
cd IntegrationPlatform/IntegrationPlatform.Web
npm install
npm run dev
```

Build:

```bash
dotnet build IntegrationPlatform/IntegrationPlatform.Api/IntegrationPlatform.Api.csproj
cd IntegrationPlatform/IntegrationPlatform.Web
npm run build
```

## Deploy

O diretório `deploy/` possui arquivos para publicação em containers:

- `deploy/docker-compose.prod.yml`;
- `deploy/docker/api.Dockerfile`;
- `deploy/docker/web.Dockerfile`;
- `deploy/nginx/integration-platform.conf.example`;
- `deploy/nginx/web.nginx.conf`.

Em produção, o reverse proxy deve encaminhar:

- `/api/` para a API;
- demais rotas para o frontend.

## Pontos de Destaque Técnico

- Pipeline engine próprio com múltiplos tipos de etapa.
- Execução com contexto compartilhado entre etapas.
- Templates dinâmicos para requests e scripts.
- Debug passo a passo de pipelines.
- Logs detalhados por execução.
- Suporte a fila persistida.
- Integração OIDC com Identity Management.
- Sincronização automática de recursos protegidos via Archon.
- Interface administrativa completa para configuração e operação.

## Status

O projeto está funcional como plataforma de integração para o ecossistema Mainstay/Archon, incluindo cadastro de conectores, composição de pipelines, execução monitorada, debug operacional e autenticação integrada ao Identity Management.

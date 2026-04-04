# Integration Platform

Plataforma de orquestração de integrações construída sobre o `Archon`.

O sistema foi desenhado para permitir a modelagem, configuração, execução e observabilidade de fluxos de integração entre sistemas externos por meio de pipelines reutilizáveis.

## Visão Geral

Na prática, o `integration-platform` resolve estes problemas:

- cadastro de tipos de integração;
- configuração de conectores concretos;
- definição de pipelines com etapas ordenadas;
- execução manual, em fila ou em modo debug;
- acompanhamento de logs, erros e resultados;
- mapeamento de referências entre IDs internos e externos.

O motor da plataforma suporta hoje três tipos de etapa:

- chamada HTTP;
- função JavaScript;
- execução de script SQL.

## Arquitetura

O projeto está organizado em:

- `IntegrationPlataform.Api`
  camada HTTP com controllers, autenticação opcional via `IdentityManagement` e integração com `Archon.Api`.
- `IntegrationPlataform.Application`
  requests, modelos e interfaces dos services.
- `IntegrationPlataform.Domain`
  entidades e enums do domínio de integração.
- `IntegrationPlataform.Infrastructure`
  persistência, migrations, services CRUD e motor de execução.
- `IntegrationPlataform.Web`
  frontend React para configuração e operação da plataforma.

## Modelo de Domínio

As entidades principais do sistema são:

- `IntegrationCategory`
  categoria administrativa para organizar integrações.
- `Integration`
  define o tipo de integração e seu identificador lógico.
- `IntegrationAttribute`
  define os campos configuráveis da integração.
- `Connector`
  instância concreta de integração usada na execução.
- `ConnectorAttributeValue`
  valores configurados do conector.
- `ApiCall`
  definição reutilizável de chamada HTTP.
- `JavaScriptFunction`
  função JS executável no pipeline.
- `DatabaseConnection`
  conexão com banco externo.
- `DatabaseScript`
  script SQL reutilizável.
- `Pipeline`
  fluxo executável dentro de uma integração.
- `PipelineStep`
  etapa do pipeline com ordem, tipo e ação em caso de erro.
- `ProcessingQueue`
  fila persistida de processamento.
- `Execution`
  registro de execução do pipeline.
- `ExecutionLog`
  log detalhado da execução.
- `Reference`
  mapeamento entre IDs internos e externos.
- `PipelineRoutine`
  rotina agendada de pipeline.

## Motor de Execução

O núcleo de execução está em:

- `ExecutionEngineService`
- `StepExecutorService`
- `QueueProcessorService`

Fluxo básico:

1. carrega conector, pipeline e etapas;
2. monta o contexto da execução com payload, atributos e variáveis;
3. executa as etapas ativas em ordem;
4. registra logs por etapa;
5. extrai variáveis do resultado para uso nas etapas seguintes;
6. finaliza a execução com status `Success`, `Error` ou `Partial`.

O comportamento em caso de erro depende de `PipelineStep.ErrorAction`:

- `Stop`
  interrompe o pipeline;
- `Continue`
  registra a falha e segue para a próxima etapa.

## Tipos de Etapa

### HTTP Request

Usa `ApiCall` com:

- método;
- URL;
- headers com template;
- body com template.

O executor interpola os dados do contexto antes de enviar a requisição.

### JavaScript Function

Usa `Jint` para executar funções JavaScript com:

- timeout;
- limite de memória;
- limite de recursão.

O script recebe acesso a:

- `variables`
- `payload`
- `attributes`
- `result`

### Execute Script

Executa SQL em banco externo a partir de `DatabaseScript`.

Suporte efetivo no código atual:

- PostgreSQL
- SQL Server

Proteções implementadas:

- `UPDATE` sem `WHERE` é bloqueado;
- `DELETE` sem `WHERE` é bloqueado.

## Modos de Execução

O sistema suporta:

- execução manual de pipeline;
- execução por identificador lógico da integração e do pipeline;
- enfileiramento para processamento posterior;
- debug passo a passo com sessão temporária em memória.

Endpoints relevantes:

- `POST /api/executions/execute`
- `POST /api/executions/execute-by-identifier/{integrationIdentifier}/{pipelineIdentifier}`
- `POST /api/executions/debug/start`
- `POST /api/executions/debug/next-step`
- `POST /api/executions/debug/finish`
- `POST /api/processingqueues/enqueue`
- `POST /api/processingqueues/{processingQueueId}/process`

## Frontend

O frontend usa:

- React
- TypeScript
- Vite
- `archon-ui`

Ele é integrado ao `IdentityManagement` para autenticação e oferece módulos de:

- dashboard;
- categorias;
- integrações;
- conectores;
- pipelines;
- conexões de banco;
- scripts SQL;
- chamadas de API;
- funções JavaScript;
- automação;
- execuções;
- fila;
- referências.

Também existe suporte visual para debug detalhado de pipelines.

## Dashboard

O dashboard atual consolida:

- integrações ativas;
- conectores ativos;
- pipelines ativos;
- execuções do dia;
- erros do dia;
- taxa de sucesso;
- execuções mensais;
- execuções recentes.

## Configuração

O backend pode operar com autenticação integrada ao `IdentityManagement` quando a configuração estiver presente.

Pontos de configuração relevantes:

- `TenantDatabases`
- `IdentityManagement`
- `Jwt`
- `RunMigrations`

## Migrations

A migration principal cria a base do sistema com tabelas para:

- integrações e categorias;
- atributos e conectores;
- chamadas API;
- funções JavaScript;
- conexões e scripts SQL;
- pipelines e etapas;
- fila;
- execuções e logs;
- referências;
- rotinas.
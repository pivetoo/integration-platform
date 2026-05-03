# Integration Platform Web

Frontend React da Integration Platform.

Este projeto contém a interface administrativa e operacional para configurar integrações, conectores, pipelines, execuções, filas, referências e rotinas de automação.

Para a documentação completa do sistema, consulte o README principal em:

```text
../../README.md
```

## Stack

- React
- TypeScript
- Vite
- React Router
- Tailwind CSS
- Archon UI

## Configuração

Exemplo de variáveis de ambiente:

```env
VITE_API_BASE_URL=https://localhost:7078/api
VITE_IDENTITY_MANAGEMENT_URL=https://auth.localhost
VITE_OIDC_CLIENT_ID=integration-platform-dev
```

## Execução

```bash
npm install
npm run dev
```

## Build

```bash
npm run build
```

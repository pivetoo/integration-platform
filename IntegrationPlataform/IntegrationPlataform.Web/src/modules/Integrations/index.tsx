import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Badge, Button, Card, CardContent, CardHeader, CardTitle, PageLayout, useApi, useI18n } from 'archon-ui'
import { ArrowRight, Cable, Plus, Workflow } from 'lucide-react'
import { integrationService } from '../../services/integrationService'
import { pipelineService } from '../../services/pipelineService'
import type { Integration } from '../../types/integration'
import type { Pipeline } from '../../types/pipeline'
import type { PaginatedResult } from '../../types/pagination'

interface IntegrationWorkspaceCard extends Integration {
  pipelinesCount: number
}

export default function IntegrationsWorkspace() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [integrations, setIntegrations] = useState<Integration[]>([])
  const [pipelines, setPipelines] = useState<Pipeline[]>([])

  const { execute: fetchIntegrations, loading } = useApi<PaginatedResult<Integration>>({
    showErrorMessage: true,
  })

  const { execute: fetchPipelines } = useApi<PaginatedResult<Pipeline>>({
    showErrorMessage: true,
  })

  useEffect(() => {
    void fetchIntegrations(() => integrationService.getAll()).then((result) => {
      if (result) {
        setIntegrations(result.data)
      }
    })

    void fetchPipelines(() => pipelineService.getAll()).then((result) => {
      if (result) {
        setPipelines(result.data)
      }
    })
  }, [])

  const workspaceCards = useMemo<IntegrationWorkspaceCard[]>(() => {
    return integrations.map((integration) => ({
      ...integration,
      pipelinesCount: pipelines.filter((pipeline) => pipeline.integrationId === integration.id).length,
    }))
  }, [integrations, pipelines])

  return (
    <PageLayout
      title="Integrações"
      onRefresh={() => {
        void fetchIntegrations(() => integrationService.getAll()).then((result) => result && setIntegrations(result.data))
        void fetchPipelines(() => pipelineService.getAll()).then((result) => result && setPipelines(result.data))
      }}
      actions={[
        {
          key: 'nova-integracao',
          label: 'Nova integração',
          icon: <Plus size={16} />,
          onClick: () => navigate('/configuracao-tecnica/integracoes'),
        },
      ]}
    >
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {workspaceCards.map((integration) => (
          <Card key={integration.id} className="transition-shadow hover:shadow-md">
            <CardHeader className="space-y-3">
              <div className="flex items-start justify-between gap-3">
                <div className="space-y-1">
                  <CardTitle className="text-lg">{integration.name}</CardTitle>
                  <p className="text-sm text-muted-foreground">{integration.description || integration.identifier}</p>
                </div>
                <Badge variant={integration.isActive ? 'success' : 'secondary'}>
                  {integration.isActive ? t('common.status.active') : t('common.status.inactive')}
                </Badge>
              </div>

              <div className="flex flex-wrap gap-2 text-xs text-muted-foreground">
                <span className="inline-flex items-center gap-1 rounded-md bg-muted px-2 py-1">
                  <Cable size={14} />
                  {integration.integrationCategory?.name || 'Sem categoria'}
                </span>
                <span className="inline-flex items-center gap-1 rounded-md bg-muted px-2 py-1">
                  <Workflow size={14} />
                  {integration.pipelinesCount} pipeline(s)
                </span>
              </div>
            </CardHeader>

            <CardContent className="space-y-4">
              <p className="text-sm text-muted-foreground">
                Gerencie o fluxo visual, os exemplos e os previews desta integração sem entrar na configuração técnica completa.
              </p>

              <div className="flex items-center justify-between gap-2">
                <Button variant="outline" onClick={() => navigate(`/configuracao-tecnica/integracoes/${integration.id}`)}>
                  Configuração técnica
                </Button>
                <Button onClick={() => navigate(`/integracoes/${integration.id}`)}>
                  Abrir workspace
                  <ArrowRight size={16} className="ml-2" />
                </Button>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {!loading && workspaceCards.length === 0 && (
        <div className="rounded-lg border border-dashed p-12 text-center text-muted-foreground">
          Nenhuma integração encontrada.
        </div>
      )}
    </PageLayout>
  )
}

import { useEffect, useMemo, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle, Badge, Button, GlobalLoader, LineChart, PieChart, BarChart, ChartContainer, useApi, useAuth, useI18n } from 'archon-ui';
import type { DashboardData } from '../../types/dashboard';
import { Cable, Plug, GitBranch, ListOrdered, Play, CheckCircle2, XCircle, AlertTriangle, Timer, TrendingUp, PieChart as PieChartIcon, BarChart3, Clock, RefreshCw } from 'lucide-react';
import { dashboardService } from '../../services/dashboardService';
import { ExecutionStatusLabels } from '../../types/execution';
import type { ExecutionStatus } from '../../types/execution';
import { ProcessingStatus, ProcessingStatusLabels } from '../../types/processingQueue';
import { formatDuration, formatTime } from '../../utils/formatters';

const statusVariantMap: Record<number, 'success' | 'destructive' | 'warning'> = {
  1: 'warning',
  2: 'success',
  3: 'destructive',
  4: 'warning',
};

const statusBorderMap: Record<number, string> = {
  1: 'border-l-amber-500',
  2: 'border-l-green-500',
  3: 'border-l-red-500',
  4: 'border-l-amber-500',
};

const queueStatusOrder = [
  ProcessingStatus.Pending,
  ProcessingStatus.Processing,
  ProcessingStatus.Completed,
  ProcessingStatus.Error,
  ProcessingStatus.Cancelled,
];

const queueStatusColors: Record<number, string> = {
  [ProcessingStatus.Pending]: '#f59e0b',
  [ProcessingStatus.Processing]: '#3b82f6',
  [ProcessingStatus.Completed]: '#10b981',
  [ProcessingStatus.Error]: '#ef4444',
  [ProcessingStatus.Cancelled]: '#9ca3af',
};

function getStatusIcon(status: number) {
  if (status === 2) {
    return <CheckCircle2 className="h-3.5 w-3.5" />;
  }
  if (status === 3) {
    return <XCircle className="h-3.5 w-3.5" />;
  }
  return <Clock className="h-3.5 w-3.5 animate-spin" />;
}

function buildGreeting(t: (key: string) => string, firstName: string | undefined): string {
  const hour = new Date().getHours();
  const period = hour < 5 ? 'dawn' : hour < 12 ? 'morning' : hour < 18 ? 'afternoon' : 'evening';
  const base = t(`dashboard.greeting.${period}`);
  return firstName ? `${base}, ${firstName}!` : `${base}!`;
}

export default function Dashboard() {
  const { t } = useI18n();
  const { user } = useAuth();
  const [data, setData] = useState<DashboardData | null>(null);
  const [isInitialLoading, setIsInitialLoading] = useState(true);

  const { execute: fetchData, loading } = useApi<DashboardData>({
    showErrorMessage: true,
  });

  const firstName = useMemo(() => user?.name?.trim().split(/\s+/)[0], [user?.name]);
  const greeting = useMemo(() => buildGreeting(t, firstName), [t, firstName]);

  const loadData = async () => {
    const result = await fetchData(() => dashboardService.getData());
    if (result) {
      setData(result);
    }
    setIsInitialLoading(false);
  };

  useEffect(() => {
    void loadData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const kpis = data?.kpis;

  const chartMensais = (data?.execucoesMensais || []).map(e => ({
    name: e.mes,
    sucesso: e.sucesso ?? 0,
    erro: e.erro ?? 0,
  }));

  const execucoesRecentes = data?.execucoesRecentes || [];

  const filaPorStatusOrdenada = useMemo(() => {
    const byStatus = new Map((data?.filaPorStatus || []).map(item => [item.status, item.count]));
    return queueStatusOrder
      .map(status => ({ status, count: byStatus.get(status) ?? 0 }))
      .filter(item => item.count > 0);
  }, [data]);

  const filaPorStatus = filaPorStatusOrdenada.map(item => ({ name: ProcessingStatusLabels[item.status], value: item.count }));
  const filaPorStatusCores = filaPorStatusOrdenada.map(item => queueStatusColors[item.status]);

  const topConectores = (data?.topConectores || []).map(item => ({
    name: item.conector,
    value: item.executionCount,
  }));

  const errosHoje = kpis?.errosHoje ?? 0;

  if (isInitialLoading) {
    return <GlobalLoader isVisible={true} className="bg-background" />;
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div className="border-l-4 border-primary pl-5">
          <h1 className="text-3xl font-bold text-foreground tracking-tight">
            <strong className="text-primary">{greeting}</strong>
          </h1>
          <p className="text-lg text-muted-foreground mt-3 leading-relaxed">{t('dashboard.subtitle')}</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <div className="flex flex-wrap items-center gap-2">
            <div className="flex items-center gap-2 rounded-full border border-border/70 bg-muted/30 px-3 py-1.5 text-xs">
              <Play className="h-3.5 w-3.5 text-amber-600" />
              <span className="font-medium text-muted-foreground">{t('dashboard.kpi.executionsToday.title')}</span>
              <span className="font-semibold text-foreground">{kpis?.execucoesHoje ?? 0}</span>
            </div>
            <div className="flex items-center gap-2 rounded-full border border-border/70 bg-muted/30 px-3 py-1.5 text-xs">
              <CheckCircle2 className="h-3.5 w-3.5 text-emerald-600" />
              <span className="font-medium text-muted-foreground">{t('dashboard.kpi.successRate.title')}</span>
              <span className="font-semibold text-foreground">{kpis ? `${kpis.taxaSucesso}%` : '-'}</span>
            </div>
            <div className={`flex items-center gap-2 rounded-full border px-3 py-1.5 text-xs ${errosHoje > 0 ? 'border-red-300 bg-red-50 dark:border-red-900 dark:bg-red-950/30' : 'border-border/70 bg-muted/30'}`}>
              <AlertTriangle className="h-3.5 w-3.5 text-red-600" />
              <span className="font-medium text-muted-foreground">{t('dashboard.kpi.errorsToday.title')}</span>
              <span className="font-semibold text-foreground">{errosHoje}</span>
            </div>
            <div className="flex items-center gap-2 rounded-full border border-border/70 bg-muted/30 px-3 py-1.5 text-xs">
              <Timer className="h-3.5 w-3.5 text-cyan-600" />
              <span className="font-medium text-muted-foreground">{t('dashboard.kpi.avgDuration.title')}</span>
              <span className="font-semibold text-foreground">{formatDuration(kpis?.duracaoMediaHojeMs)}</span>
            </div>
          </div>
          <Button variant="outline" size="sm" onClick={() => void loadData()} disabled={loading}>
            <RefreshCw className={`mr-1 h-3.5 w-3.5 ${loading ? 'animate-spin' : ''}`} /> {t('dashboard.refresh')}
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-4">
        <Card>
          <CardContent className="pt-5 pb-5 flex items-start justify-between">
            <div>
              <p className="text-xs uppercase tracking-wide text-muted-foreground">{t('dashboard.kpi.integrations.title')}</p>
              <p className="text-2xl font-semibold mt-1">{kpis?.integracoesAtivas ?? '-'}</p>
              <p className="text-[10px] text-muted-foreground">{t('dashboard.kpi.integrations.subtitle')}</p>
            </div>
            <span className="rounded-md bg-blue-500/15 p-2 text-blue-600"><Cable size={18} /></span>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-5 pb-5 flex items-start justify-between">
            <div>
              <p className="text-xs uppercase tracking-wide text-muted-foreground">{t('dashboard.kpi.connectors.title')}</p>
              <p className="text-2xl font-semibold mt-1">{kpis?.conectoresAtivos ?? '-'}</p>
              <p className="text-[10px] text-muted-foreground">{t('dashboard.kpi.connectors.subtitle')}</p>
            </div>
            <span className="rounded-md bg-violet-500/15 p-2 text-violet-600"><Plug size={18} /></span>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-5 pb-5 flex items-start justify-between">
            <div>
              <p className="text-xs uppercase tracking-wide text-muted-foreground">{t('dashboard.kpi.pipelines.title')}</p>
              <p className="text-2xl font-semibold mt-1">{kpis?.pipelinesAtivos ?? '-'}</p>
              <p className="text-[10px] text-muted-foreground">{t('dashboard.kpi.pipelines.subtitle')}</p>
            </div>
            <span className="rounded-md bg-cyan-500/15 p-2 text-cyan-600"><GitBranch size={18} /></span>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-5 pb-5 flex items-start justify-between">
            <div>
              <p className="text-xs uppercase tracking-wide text-muted-foreground">{t('dashboard.kpi.queuePending.title')}</p>
              <p className="text-2xl font-semibold mt-1">{kpis?.filaPendente ?? '-'}</p>
              <p className="text-[10px] text-muted-foreground">{t('dashboard.kpi.queuePending.subtitle')}</p>
            </div>
            <span className="rounded-md bg-amber-500/15 p-2 text-amber-600"><ListOrdered size={18} /></span>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 xl:grid-cols-[1.4fr_0.9fr]">
        <Card className="overflow-hidden border border-border/70 shadow-sm">
          <CardHeader className="border-b bg-muted/20 pb-4">
            <CardTitle className="flex flex-wrap items-center justify-between gap-2 text-base">
              <span className="flex items-center gap-2">
                <TrendingUp className="h-5 w-5 text-primary" />
                {t('dashboard.monthlyExecutions')}
              </span>
              <span className="flex flex-wrap items-center gap-3 text-xs font-normal text-muted-foreground">
                <span className="flex items-center gap-1.5">
                  <span className="h-2 w-2 rounded-full" style={{ backgroundColor: '#10b981' }} />
                  {t('dashboard.monthlyExecutions.series.success')}
                </span>
                <span className="flex items-center gap-1.5">
                  <span className="h-2 w-2 rounded-full" style={{ backgroundColor: '#ef4444' }} />
                  {t('dashboard.monthlyExecutions.series.error')}
                </span>
              </span>
            </CardTitle>
          </CardHeader>
          <CardContent className="p-5">
            <ChartContainer height={290}>
              <LineChart
                data={chartMensais}
                dataKeys={['sucesso', 'erro']}
                colors={['#10b981', '#ef4444']}
                height={230}
                showLegend={false}
                showDots={false}
                enableArea
                areaOpacity={0.08}
              />
            </ChartContainer>
          </CardContent>
        </Card>

        <Card className="overflow-hidden border border-border/70 shadow-sm">
          <CardHeader className="border-b bg-muted/20 pb-4">
            <CardTitle className="flex items-center gap-2 text-base">
              <PieChartIcon className="h-5 w-5 text-violet-600" />
              {t('dashboard.queueByStatus.title')}
            </CardTitle>
          </CardHeader>
          <CardContent className="p-5">
            <ChartContainer height={290} isEmpty={filaPorStatus.length === 0} emptyMessage={t('dashboard.queueByStatus.empty')}>
              <PieChart
                data={filaPorStatus}
                colors={filaPorStatusCores}
                height={250}
                innerRadius={55}
              />
            </ChartContainer>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <Card className="border border-border/70 shadow-sm">
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-sm font-semibold">
              <BarChart3 className="h-4 w-4 text-violet-600" />
              {t('dashboard.topConnectors.title')}
            </CardTitle>
            <p className="text-xs text-muted-foreground">{t('dashboard.topConnectors.subtitle')}</p>
          </CardHeader>
          <CardContent className="px-4 pb-4">
            <ChartContainer height={220} isEmpty={topConectores.length === 0} emptyMessage={t('dashboard.topConnectors.empty')}>
              <BarChart
                data={topConectores}
                dataKeys={['value']}
                colors={['#8b5cf6']}
                height={200}
                layout="horizontal"
                showLegend={false}
              />
            </ChartContainer>
          </CardContent>
        </Card>

        <Card className="border border-border/70 shadow-sm">
          <CardHeader className="flex flex-row items-center gap-2 pb-4">
            <Clock className="h-5 w-5 text-muted-foreground" />
            <CardTitle className="text-base font-semibold">{t('dashboard.recentExecutions')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {execucoesRecentes.map((exec) => (
                <div
                  key={exec.id}
                  className={`flex items-center justify-between rounded-lg border-l-4 bg-muted/30 px-4 py-3 ${statusBorderMap[exec.status] || 'border-l-gray-400'}`}
                >
                  <div className="space-y-0.5">
                    <p className="text-sm font-medium">{exec.pipeline}</p>
                    <p className="text-xs text-muted-foreground">{exec.conector}</p>
                  </div>
                  <div className="text-right space-y-0.5">
                    <Badge variant={statusVariantMap[exec.status] || 'warning'}>
                      <span className="flex items-center gap-1">
                        {getStatusIcon(exec.status)}
                        {ExecutionStatusLabels[exec.status as ExecutionStatus] || '-'}
                      </span>
                    </Badge>
                    <div className="flex items-center gap-2 justify-end">
                      <span className="text-xs text-muted-foreground">{formatDuration(exec.duration)}</span>
                      <span className="text-xs text-muted-foreground">{formatTime(exec.startedAt)}</span>
                    </div>
                  </div>
                </div>
              ))}
              {execucoesRecentes.length === 0 && (
                <p className="text-sm text-muted-foreground text-center py-4">{t('dashboard.noRecentExecutions')}</p>
              )}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

import { useMemo, useEffect, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle, GlobalLoader, LineChart, PieChart, ChartContainer, useApi, useI18n } from 'archon-ui';
import type { DashboardData } from '../../types/dashboard';
import { LayoutDashboard, Cable, Plug, GitBranch, ListOrdered, Play, CheckCircle2, AlertTriangle, Timer, TrendingUp, PieChart as PieChartIcon } from 'lucide-react';
import { dashboardService } from '../../services/dashboardService';
import { ProcessingStatus, ProcessingStatusLabels } from '../../types/processingQueue';
import { formatDuration } from '../../utils/formatters';

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

export default function Dashboard() {
  const { t } = useI18n();
  const [data, setData] = useState<DashboardData | null>(null);
  const [isInitialLoading, setIsInitialLoading] = useState(true);

  const { execute: fetchData } = useApi<DashboardData>({
    showErrorMessage: true,
  });

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

  const filaPorStatusOrdenada = useMemo(() => {
    const byStatus = new Map((data?.filaPorStatus || []).map(item => [item.status, item.count]));
    return queueStatusOrder
      .map(status => ({ status, count: byStatus.get(status) ?? 0 }))
      .filter(item => item.count > 0);
  }, [data]);

  const filaPorStatus = filaPorStatusOrdenada.map(item => ({ name: ProcessingStatusLabels[item.status], value: item.count }));
  const filaPorStatusCores = filaPorStatusOrdenada.map(item => queueStatusColors[item.status]);

  const errosHoje = kpis?.errosHoje ?? 0;

  if (isInitialLoading) {
    return <GlobalLoader isVisible={true} className="bg-background" />;
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="flex items-center gap-3">
        <LayoutDashboard className="h-8 w-8 text-primary" />
        <h1 className="text-3xl font-bold">{t('dashboard.title')}</h1>
      </div>

      <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
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

        <Card>
          <CardContent className="pt-5 pb-5 flex items-start justify-between">
            <div>
              <p className="text-xs uppercase tracking-wide text-muted-foreground">{t('dashboard.kpi.executionsToday.title')}</p>
              <p className="text-2xl font-semibold mt-1">{kpis?.execucoesHoje ?? '-'}</p>
              <p className="text-[10px] text-muted-foreground">{t('dashboard.kpi.executionsToday.subtitle')}</p>
            </div>
            <span className="rounded-md bg-amber-500/15 p-2 text-amber-600"><Play size={18} /></span>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-5 pb-5 flex items-start justify-between">
            <div>
              <p className="text-xs uppercase tracking-wide text-muted-foreground">{t('dashboard.kpi.successRate.title')}</p>
              <p className="text-2xl font-semibold mt-1">{kpis ? `${kpis.taxaSucesso}%` : '-'}</p>
              <p className="text-[10px] text-muted-foreground">{t('dashboard.kpi.successRate.subtitle')}</p>
            </div>
            <span className="rounded-md bg-emerald-500/15 p-2 text-emerald-600"><CheckCircle2 size={18} /></span>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-5 pb-5 flex items-start justify-between">
            <div>
              <p className="text-xs uppercase tracking-wide text-muted-foreground">{t('dashboard.kpi.errorsToday.title')}</p>
              <p className={`text-2xl font-semibold mt-1 ${errosHoje > 0 ? 'text-red-600' : ''}`}>{errosHoje}</p>
              <p className="text-[10px] text-muted-foreground">{t('dashboard.kpi.errorsToday.subtitle')}</p>
            </div>
            <span className="rounded-md bg-red-500/15 p-2 text-red-600"><AlertTriangle size={18} /></span>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-5 pb-5 flex items-start justify-between">
            <div>
              <p className="text-xs uppercase tracking-wide text-muted-foreground">{t('dashboard.kpi.avgDuration.title')}</p>
              <p className="text-2xl font-semibold mt-1">{formatDuration(kpis?.duracaoMediaHojeMs)}</p>
              <p className="text-[10px] text-muted-foreground">{t('dashboard.kpi.avgDuration.subtitle')}</p>
            </div>
            <span className="rounded-md bg-indigo-500/15 p-2 text-indigo-600"><Timer size={18} /></span>
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
    </div>
  );
}

import { useState, useEffect } from 'react';
import {
  Card, CardContent, CardHeader, CardTitle, Badge,
  AreaChart, ChartContainer, useApi
} from 'archon-ui';
import {
  LayoutDashboard, Cable, Plug, GitBranch, Play, CheckCircle,
  XCircle, Clock, TrendingUp, AlertTriangle, Zap
} from 'lucide-react';
import { dashboardService } from '../services/dashboardService';
import { StatusExecucaoLabels } from '../types/execucao';
import type { DashboardData, ExecucaoRecente } from '../types/dashboard';
import type { StatusExecucao } from '../types/execucao';

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

function getStatusIcon(status: number) {
  if (status === 2) {
    return <CheckCircle className="h-3.5 w-3.5" />;
  }
  if (status === 3) {
    return <XCircle className="h-3.5 w-3.5" />;
  }
  return <Clock className="h-3.5 w-3.5 animate-spin" />;
}

function formatDuracao(ms?: number): string {
  if (ms == null) {
    return '-';
  }
  if (ms < 1000) {
    return `${ms}ms`;
  }
  const seconds = Math.floor(ms / 1000);
  if (seconds < 60) {
    return `${seconds}s`;
  }
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  return `${minutes}m ${remainingSeconds}s`;
}

function formatHorario(dateStr: string): string {
  return new Date(dateStr).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
}

export default function Dashboard() {
  const [data, setData] = useState<DashboardData | null>(null);

  const { execute: fetchData } = useApi<DashboardData>({
    showErrorMessage: true,
  });

  const loadData = async () => {
    const result = await fetchData(() => dashboardService.getData());
    if (result) {
      setData(result);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const kpis = data?.kpis;

  const chartMensais = (data?.execucoesMensais || []).map(e => ({
    name: e.mes,
    sucesso: e.sucesso,
    erro: e.erro,
  }));

  const execucoesRecentes = data?.execucoesRecentes || [];

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <LayoutDashboard className="h-8 w-8 text-primary" />
        <h1 className="text-3xl font-bold">Dashboard</h1>
      </div>

      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
        <Card className="border-l-4 border-l-blue-500">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Integrações</CardTitle>
            <Cable className="h-5 w-5 text-blue-500" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-blue-600">{kpis?.integracoesAtivas ?? '-'}</div>
            <p className="text-xs text-muted-foreground">ativas</p>
          </CardContent>
        </Card>

        <Card className="border-l-4 border-l-violet-500">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Conectores</CardTitle>
            <Plug className="h-5 w-5 text-violet-500" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-violet-600">{kpis?.conectoresAtivos ?? '-'}</div>
            <p className="text-xs text-muted-foreground">ativos</p>
          </CardContent>
        </Card>

        <Card className="border-l-4 border-l-cyan-500">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Pipelines</CardTitle>
            <GitBranch className="h-5 w-5 text-cyan-500" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-cyan-600">{kpis?.pipelinesAtivos ?? '-'}</div>
            <p className="text-xs text-muted-foreground">ativos</p>
          </CardContent>
        </Card>

        <Card className="border-l-4 border-l-amber-500">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Execuções Hoje</CardTitle>
            <Play className="h-5 w-5 text-amber-500" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-amber-600">{kpis?.execucoesHoje ?? '-'}</div>
            <p className="text-xs text-muted-foreground">processadas</p>
          </CardContent>
        </Card>

        <Card className="border-l-4 border-l-green-500">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Taxa Sucesso</CardTitle>
            <Zap className="h-5 w-5 text-green-500" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-green-600">{kpis ? `${kpis.taxaSucesso}%` : '-'}</div>
            <p className="text-xs text-muted-foreground">hoje</p>
          </CardContent>
        </Card>

        <Card className="border-l-4 border-l-red-500">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Erros Hoje</CardTitle>
            <AlertTriangle className="h-5 w-5 text-red-500" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-red-600">{kpis?.errosHoje ?? '-'}</div>
            <p className="text-xs text-muted-foreground">falhas</p>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 items-start">
        <Card>
          <ChartContainer title="Execuções Mensais" icon={<TrendingUp className="h-5 w-5" />} height={300}>
            <AreaChart
              data={chartMensais}
              dataKeys={['sucesso', 'erro']}
              xAxisKey="name"
              colors={['#10b981', '#ef4444']}
              height={250}
              showGrid
              showLegend
              showTooltip
            />
          </ChartContainer>
        </Card>

        <Card className="min-h-[365px]">
          <CardHeader className="flex flex-row items-center gap-2 pb-4">
            <Clock className="h-5 w-5 text-muted-foreground" />
            <CardTitle className="text-base font-semibold">Execuções Recentes</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {execucoesRecentes.map((exec: ExecucaoRecente) => (
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
                        {StatusExecucaoLabels[exec.status as StatusExecucao] || '-'}
                      </span>
                    </Badge>
                    <div className="flex items-center gap-2 justify-end">
                      <span className="text-xs text-muted-foreground">{formatDuracao(exec.duracao)}</span>
                      <span className="text-xs text-muted-foreground">{formatHorario(exec.iniciadoEm)}</span>
                    </div>
                  </div>
                </div>
              ))}
              {execucoesRecentes.length === 0 && (
                <p className="text-sm text-muted-foreground text-center py-4">Nenhuma execução recente</p>
              )}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

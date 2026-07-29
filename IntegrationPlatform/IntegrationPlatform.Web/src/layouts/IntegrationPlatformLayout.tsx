import { useMemo } from 'react';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import { AppLayout, useAuth, useNotifications, AuthService, useI18n } from 'archon-ui';
import type { BreadcrumbItem, ModuleNavConfig } from 'archon-ui';
import { FolderTree, GitBranch, Plug2, Workflow, Database, FileJson, Globe, Code2, Play, ListOrdered, ExternalLink, Clock3, Layers, LayoutDashboard, FileBarChart2, ScrollText } from 'lucide-react';
import logoEmpresa from '../assets/logo-empresa.png';

export default function IntegrationPlatformLayout() {
  const { t } = useI18n();
  const { user: authUser, contract } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const { notifications, markAsRead, markAllAsRead, clearAll } = useNotifications({ enabled: false });

  const handleLogout = async () => {
    try {
      await AuthService.logoutFromServer();
    } finally {
      window.location.href = new URL('/logout', import.meta.env.VITE_IDENTITY_MANAGEMENT_URL).toString();
    }
  };

  const moduleNav: ModuleNavConfig = [
    {
      key: 'geral',
      label: t('layout.menu.general'),
      icon: <LayoutDashboard size={20} />,
      group: 'op',
      routes: [
        { key: 'dashboard', label: t('layout.menu.dashboard'), path: '/', icon: <LayoutDashboard size={20} /> },
      ],
    },
    {
      key: 'integracoes',
      label: t('layout.menu.integrations'),
      icon: <GitBranch size={20} />,
      group: 'op',
      routes: [
        { key: 'categorias', label: t('layout.menu.categories'), path: '/categorias', icon: <FolderTree size={20} /> },
        { key: 'contratos-servico', label: t('layout.menu.serviceContracts'), path: '/contratos-servico', icon: <ScrollText size={20} /> },
        { key: 'integracoes', label: t('layout.menu.integrations'), path: '/integracoes', icon: <GitBranch size={20} /> },
        { key: 'conectores', label: t('layout.menu.connectors'), path: '/conectores', icon: <Plug2 size={20} /> },
      ],
    },
    {
      key: 'recursos',
      label: t('layout.menu.resources'),
      icon: <Layers size={20} />,
      group: 'op',
      routes: [
        { key: 'chamadas-api', label: t('layout.menu.apiCalls'), path: '/chamadas-api', icon: <Globe size={20} /> },
        { key: 'funcoes-javascript', label: t('layout.menu.javaScriptFunctions'), path: '/funcoes-javascript', icon: <Code2 size={20} /> },
        { key: 'conexoes-banco', label: t('layout.menu.databaseConnections'), path: '/conexoes-banco', icon: <Database size={20} /> },
        { key: 'scripts-banco-dados', label: t('layout.menu.databaseScripts'), path: '/scripts-banco-dados', icon: <FileJson size={20} /> },
        { key: 'pipelines', label: t('layout.menu.pipelines'), path: '/pipelines', icon: <Workflow size={20} /> },
      ],
    },
    {
      key: 'operacional',
      label: t('layout.menu.operational'),
      icon: <Play size={20} />,
      group: 'op',
      routes: [
        { key: 'execucoes', label: t('layout.menu.executions'), path: '/execucoes', icon: <Play size={20} /> },
        { key: 'fila', label: t('layout.menu.processingQueue'), path: '/fila', icon: <ListOrdered size={20} /> },
        { key: 'referencias', label: t('layout.menu.references'), path: '/referencias', icon: <ExternalLink size={20} /> },
      ],
    },
    {
      key: 'automacao',
      label: t('layout.menu.automation'),
      icon: <Clock3 size={20} />,
      group: 'op',
      routes: [
        { key: 'rotinas-pipeline', label: t('layout.menu.pipelineRoutines'), path: '/automacao', icon: <Clock3 size={20} /> },
      ],
    },
    {
      key: 'relatorios',
      label: t('layout.menu.reports'),
      icon: <FileBarChart2 size={20} />,
      group: 'op',
      routes: [],
    },
  ];

  const breadcrumbs = useMemo((): BreadcrumbItem[] => {
    const path = location.pathname;
    const crumbs: BreadcrumbItem[] = [
      { label: t('layout.breadcrumb.home'), onClick: () => navigate('/') }
    ];

    const routeMap: Record<string, string> = {
      '/integracoes': t('layout.menu.integrations'),
      '/categorias': t('layout.menu.categories'),
      '/contratos-servico': t('layout.menu.serviceContracts'),
      '/conectores': t('layout.menu.connectors'),
      '/chamadas-api': t('layout.menu.apiCalls'),
      '/funcoes-javascript': t('layout.menu.javaScriptFunctions'),
      '/scripts-banco-dados': t('layout.menu.databaseScripts'),
      '/conexoes-banco': t('layout.menu.databaseConnections'),
      '/pipelines': t('layout.menu.pipelines'),
      '/execucoes': t('layout.menu.executions'),
      '/fila': t('layout.menu.processingQueue'),
      '/referencias': t('layout.menu.references'),
      '/automacao': t('layout.menu.pipelineRoutines'),
    };

    const currentLabel = routeMap[path];
    if (currentLabel) {
      crumbs.push({ label: currentLabel });
    }

    if (path.match(/^\/pipelines\/\d+$/)) {
      crumbs.push({ label: t('layout.menu.pipelines'), onClick: () => navigate('/pipelines') });
      crumbs.push({ label: t('layout.breadcrumb.details') });
    }

    if (path.match(/^\/conectores\/\d+$/)) {
      crumbs.push({ label: t('layout.menu.connectors'), onClick: () => navigate('/conectores') });
      crumbs.push({ label: t('layout.breadcrumb.details') });
    }

    if (path.match(/^\/integracoes\/\d+$/)) {
      crumbs.push({ label: t('layout.menu.integrations'), onClick: () => navigate('/integracoes') });
      crumbs.push({ label: t('layout.breadcrumb.details') });
    }

    return crumbs;
  }, [location.pathname, navigate, t]);

  return (
    <AppLayout
      title="Integrations"
      subtitle="by Mainstay"
      navbarCompanyName={contract?.companyName}
      user={{
        name: authUser?.name ?? '',
        email: authUser?.email ?? '',
        role: contract?.roleName,
      }}
      onLogout={handleLogout}
      // Placa branca sob o logo: a marca e navy sobre fundo transparente e sumia no tema escuro,
      // onde o rail usa --card 220 13% 15%. No tema claro o card e branco puro, entao a placa
      // fica invisivel e nada muda.
      logo={
        <span className="flex items-center justify-center rounded-md bg-white p-0.5">
          <img src={logoEmpresa} alt="Mainstay" style={{ width: 28, height: 28, objectFit: 'contain' }} />
        </span>
      }
      navMode="module-rail"
      moduleNav={moduleNav}
      breadcrumbs={breadcrumbs}
      notifications={notifications}
      onNotificationRead={markAsRead}
      onMarkAllAsRead={markAllAsRead}
      onClearAllNotifications={clearAll}
    >
      <Outlet />
    </AppLayout>
  );
}

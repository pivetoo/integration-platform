import { useMemo } from 'react';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import { AppLayout, useAuth, useAppNavigation, useNotifications, AuthService, useI18n } from 'archon-ui';
import type { BreadcrumbItem } from 'archon-ui';
import { FolderTree, GitBranch, Plug2, Workflow, Database, FileJson, Globe, Code2, LayoutDashboard, Play, ListOrdered, ExternalLink, Clock3, Sparkles } from 'lucide-react';
import logoIntegrationHub from '../assets/logo-integration-hub.svg';
import AboutIntegrationHubModal from '../components/modals/AboutIntegrationPlatformModal';

export default function IntegrationHubLayout() {
  const { t } = useI18n();
  const { user: authUser, contract, logout } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const { createMenuGroup } = useAppNavigation({});
  const { notifications, markAsRead, markAllAsRead, clearAll } = useNotifications({ enabled: false });

  const handleLogout = async () => {
    await AuthService.logoutFromServer();
    logout();
  };

  const menuGroups = [
    createMenuGroup(t('layout.menu.general'), [
      { key: 'dashboard', label: t('layout.menu.dashboard'), path: '/', icon: <LayoutDashboard size={20} /> },
    ]),
    createMenuGroup('Integrações', [
      { key: 'workspace-integracoes', label: 'Workspace', path: '/integracoes', icon: <Sparkles size={20} /> },
    ]),
    createMenuGroup('Operação', [
      { key: 'execucoes', label: t('layout.menu.executions'), path: '/operacao/execucoes', icon: <Play size={20} /> },
      { key: 'fila', label: t('layout.menu.processingQueue'), path: '/operacao/fila', icon: <ListOrdered size={20} /> },
      { key: 'referencias', label: t('layout.menu.references'), path: '/operacao/referencias', icon: <ExternalLink size={20} /> },
    ]),
    createMenuGroup('Catálogo', [
      { key: 'categorias', label: t('layout.menu.categories'), path: '/catalogo/categorias', icon: <FolderTree size={20} /> },
    ]),
    createMenuGroup('Configuração técnica', [
      { key: 'integracoes', label: t('layout.menu.integrations'), path: '/configuracao-tecnica/integracoes', icon: <GitBranch size={20} /> },
      { key: 'conectores', label: t('layout.menu.connectors'), path: '/configuracao-tecnica/conectores', icon: <Plug2 size={20} /> },
      { key: 'pipelines', label: t('layout.menu.pipelines'), path: '/configuracao-tecnica/pipelines', icon: <Workflow size={20} /> },
      { key: 'conexoes-banco', label: t('layout.menu.databaseConnections'), path: '/configuracao-tecnica/conexoes-banco', icon: <Database size={20} /> },
      { key: 'scripts-banco-dados', label: t('layout.menu.databaseScripts'), path: '/configuracao-tecnica/scripts-banco-dados', icon: <FileJson size={20} /> },
      { key: 'chamadas-api', label: t('layout.menu.apiCalls'), path: '/configuracao-tecnica/chamadas-api', icon: <Globe size={20} /> },
      { key: 'funcoes-javascript', label: t('layout.menu.javaScriptFunctions'), path: '/configuracao-tecnica/funcoes-javascript', icon: <Code2 size={20} /> },
      { key: 'automacao', label: t('layout.menu.automation'), path: '/configuracao-tecnica/automacao', icon: <Clock3 size={20} /> },
    ]),
  ];

  const breadcrumbs = useMemo((): BreadcrumbItem[] => {
    const path = location.pathname;
    const crumbs: BreadcrumbItem[] = [
      { label: t('layout.breadcrumb.home'), onClick: () => navigate('/') }
    ];

    const routeMap: Record<string, string> = {
      '/': t('layout.menu.dashboard'),
      '/integracoes': 'Integrações',
      '/catalogo/categorias': t('layout.menu.categories'),
      '/configuracao-tecnica/integracoes': t('layout.menu.integrations'),
      '/configuracao-tecnica/conectores': t('layout.menu.connectors'),
      '/configuracao-tecnica/chamadas-api': t('layout.menu.apiCalls'),
      '/configuracao-tecnica/funcoes-javascript': t('layout.menu.javaScriptFunctions'),
      '/configuracao-tecnica/scripts-banco-dados': t('layout.menu.databaseScripts'),
      '/configuracao-tecnica/conexoes-banco': t('layout.menu.databaseConnections'),
      '/configuracao-tecnica/pipelines': t('layout.menu.pipelines'),
      '/operacao/execucoes': t('layout.menu.executions'),
      '/operacao/fila': t('layout.menu.processingQueue'),
      '/operacao/referencias': t('layout.menu.references'),
      '/configuracao-tecnica/automacao': t('layout.menu.automation'),
    };

    const currentLabel = routeMap[path];
    if (currentLabel && currentLabel !== t('layout.menu.dashboard')) {
      crumbs.push({ label: currentLabel });
    }

    if (path.match(/^\/configuracao-tecnica\/pipelines\/\d+$/)) {
      crumbs.push({ label: t('layout.menu.pipelines'), onClick: () => navigate('/configuracao-tecnica/pipelines') });
      crumbs.push({ label: t('layout.breadcrumb.details') });
    }

    if (path.match(/^\/configuracao-tecnica\/conectores\/\d+$/)) {
      crumbs.push({ label: t('layout.menu.connectors'), onClick: () => navigate('/configuracao-tecnica/conectores') });
      crumbs.push({ label: t('layout.breadcrumb.details') });
    }

    if (path.match(/^\/configuracao-tecnica\/integracoes\/\d+$/)) {
      crumbs.push({ label: t('layout.menu.integrations'), onClick: () => navigate('/configuracao-tecnica/integracoes') });
      crumbs.push({ label: t('layout.breadcrumb.details') });
    }

    if (path.match(/^\/integracoes\/\d+$/)) {
      crumbs.push({ label: 'Integrações', onClick: () => navigate('/integracoes') });
      crumbs.push({ label: t('layout.breadcrumb.details') });
    }

    return crumbs;
  }, [location.pathname, navigate, t]);

  return (
    <AppLayout
      title={contract?.systemApplicationName ?? 'IntegrationHub'}
      subtitle={contract?.companyName ?? ''}
      user={{
        name: authUser?.name ?? '',
        email: authUser?.email ?? '',
        role: contract?.roleName,
      }}
      onLogout={handleLogout}
      logo={<img src={logoIntegrationHub} alt="Logo" className="ih-sidebar-logo h-6 w-6 object-contain" />}
      menuGroups={menuGroups}
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

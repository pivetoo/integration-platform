import { useMemo } from 'react';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import {
  AppLayout,
  useAuth,
  useAppNavigation,
  useNotifications,
  AuthService,
  useI18n
} from 'archon-ui';
import type { BreadcrumbItem } from 'archon-ui';
import { FolderTree, GitBranch, Plug2, Workflow, Database, FileJson, Globe, Code2, LayoutDashboard, Play, ListOrdered, ExternalLink, Clock3 } from 'lucide-react';
import logoIntegrationHub from '../assets/logo-integration-hub.svg';
import AboutIntegrationHubModal from '../components/modals/AboutIntegrationHubModal';

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
    createMenuGroup(t('layout.menu.configuration'), [
      { key: 'categorias', label: t('layout.menu.categories'), path: '/categorias', icon: <FolderTree size={20} /> },
      { key: 'integracoes', label: t('layout.menu.integrations'), path: '/integracoes', icon: <GitBranch size={20} /> },
      { key: 'conectores', label: t('layout.menu.connectors'), path: '/conectores', icon: <Plug2 size={20} /> },
      { key: 'pipelines', label: t('layout.menu.pipelines'), path: '/pipelines', icon: <Workflow size={20} /> },
      { key: 'conexoes-banco', label: t('layout.menu.databaseConnections'), path: '/conexoes-banco', icon: <Database size={20} /> },
      { key: 'scripts-banco-dados', label: t('layout.menu.databaseScripts'), path: '/scripts-banco-dados', icon: <FileJson size={20} /> },
      { key: 'chamadas-api', label: t('layout.menu.apiCalls'), path: '/chamadas-api', icon: <Globe size={20} /> },
      { key: 'funcoes-javascript', label: t('layout.menu.javaScriptFunctions'), path: '/funcoes-javascript', icon: <Code2 size={20} /> },
      { key: 'automacao', label: t('layout.menu.automation'), path: '/automacao', icon: <Clock3 size={20} /> },
    ]),
    createMenuGroup(t('layout.menu.operational'), [
      { key: 'execucoes', label: t('layout.menu.executions'), path: '/execucoes', icon: <Play size={20} /> },
      { key: 'fila', label: t('layout.menu.processingQueue'), path: '/fila', icon: <ListOrdered size={20} /> },
      { key: 'referencias', label: t('layout.menu.references'), path: '/referencias', icon: <ExternalLink size={20} /> },
    ]),
  ];

  const breadcrumbs = useMemo((): BreadcrumbItem[] => {
    const path = location.pathname;
    const crumbs: BreadcrumbItem[] = [
      { label: t('layout.breadcrumb.home'), onClick: () => navigate('/') }
    ];

    const routeMap: Record<string, string> = {
      '/': t('layout.menu.dashboard'),
      '/integracoes': t('layout.menu.integrations'),
      '/categorias': t('layout.menu.categories'),
      '/conectores': t('layout.menu.connectors'),
      '/chamadas-api': t('layout.menu.apiCalls'),
      '/funcoes-javascript': t('layout.menu.javaScriptFunctions'),
      '/scripts-banco-dados': t('layout.menu.databaseScripts'),
      '/conexoes-banco': t('layout.menu.databaseConnections'),
      '/pipelines': t('layout.menu.pipelines'),
      '/execucoes': t('layout.menu.executions'),
      '/fila': t('layout.menu.processingQueue'),
      '/referencias': t('layout.menu.references'),
      '/automacao': t('layout.menu.automation'),
    };

    const currentLabel = routeMap[path];
    if (currentLabel && currentLabel !== t('layout.menu.dashboard')) {
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
      showAboutMenuItem
      renderAboutModal={(close) => (
        <AboutIntegrationHubModal
          open
          onOpenChange={(open) => { if (!open) { close(); } }}
          systemName={contract?.systemApplicationName ?? 'IntegrationHub'}
          companyName={contract?.companyName ?? '-'}
          userName={authUser?.name ?? '-'}
        />
      )}
    >
      <Outlet />
    </AppLayout>
  );
}

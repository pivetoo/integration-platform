import { useMemo } from 'react';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import {
  AppLayout,
  useAuth,
  useAppNavigation,
  useNotifications,
  AuthService
} from 'd-rts';
import type { BreadcrumbItem } from 'd-rts';
import { FolderTree, GitBranch, Plug2, Workflow, Database, FileJson, Globe, Code2, LayoutDashboard, Play, ListOrdered, ExternalLink, Clock3 } from 'lucide-react';
import logoIntegrationHub from '../assets/logo-integration-hub.svg';
import AboutIntegrationHubModal from '../components/modals/AboutIntegrationHubModal';
import CopilotChatWidget from '../components/chat/CopilotChatWidget';

export default function IntegrationHubLayout() {
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
    createMenuGroup('Geral', [
      { key: 'dashboard', label: 'Dashboard', path: '/', icon: <LayoutDashboard size={20} /> },
    ]),
    createMenuGroup('Configuração', [
      { key: 'categorias', label: 'Categorias', path: '/categorias', icon: <FolderTree size={20} /> },
      { key: 'integracoes', label: 'Integrações', path: '/integracoes', icon: <GitBranch size={20} /> },
      { key: 'conectores', label: 'Conectores', path: '/conectores', icon: <Plug2 size={20} /> },
      { key: 'pipelines', label: 'Pipelines', path: '/pipelines', icon: <Workflow size={20} /> },
      { key: 'conexoes-banco', label: 'Conexões Banco de Dados', path: '/conexoes-banco', icon: <Database size={20} /> },
      { key: 'scripts-banco-dados', label: 'Scripts SQL', path: '/scripts-banco-dados', icon: <FileJson size={20} /> },
      { key: 'chamadas-api', label: 'Chamadas de API', path: '/chamadas-api', icon: <Globe size={20} /> },
      { key: 'funcoes-javascript', label: 'Funções JavaScript', path: '/funcoes-javascript', icon: <Code2 size={20} /> },
      { key: 'automacao', label: 'Automação', path: '/automacao', icon: <Clock3 size={20} /> },
    ]),
    createMenuGroup('Operacional', [
      { key: 'execucoes', label: 'Execuções', path: '/execucoes', icon: <Play size={20} /> },
      { key: 'fila', label: 'Fila de Processamento', path: '/fila', icon: <ListOrdered size={20} /> },
      { key: 'referencias', label: 'Referências', path: '/referencias', icon: <ExternalLink size={20} /> },
    ]),
  ];

  const breadcrumbs = useMemo((): BreadcrumbItem[] => {
    const path = location.pathname;
    const crumbs: BreadcrumbItem[] = [
      { label: 'Início', onClick: () => navigate('/') }
    ];

    const routeMap: Record<string, string> = {
      '/': 'Dashboard',
      '/integracoes': 'Integrações',
      '/categorias': 'Categorias',
      '/conectores': 'Conectores',
      '/chamadas-api': 'Chamadas de API',
      '/funcoes-javascript': 'Funções JavaScript',
      '/scripts-banco-dados': 'Scripts Banco de Dados',
      '/conexoes-banco': 'Conexões de Banco',
      '/pipelines': 'Pipelines',
      '/execucoes': 'Execuções',
      '/fila': 'Fila de Processamento',
      '/referencias': 'Referências',
      '/automacao': 'Automação',
    };

    const currentLabel = routeMap[path];
    if (currentLabel && currentLabel !== 'Dashboard') {
      crumbs.push({ label: currentLabel });
    }

    if (path.match(/^\/pipelines\/\d+$/)) {
      crumbs.push({ label: 'Pipelines', onClick: () => navigate('/pipelines') });
      crumbs.push({ label: 'Detalhes' });
    }

    if (path.match(/^\/conectores\/\d+$/)) {
      crumbs.push({ label: 'Conectores', onClick: () => navigate('/conectores') });
      crumbs.push({ label: 'Detalhes' });
    }

    if (path.match(/^\/integracoes\/\d+$/)) {
      crumbs.push({ label: 'Integrações', onClick: () => navigate('/integracoes') });
      crumbs.push({ label: 'Detalhes' });
    }

    return crumbs;
  }, [location.pathname, navigate]);

  return (
    <>
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
      <CopilotChatWidget />
    </>
  );
}

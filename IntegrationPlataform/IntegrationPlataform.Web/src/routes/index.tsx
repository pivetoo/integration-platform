import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Callback, ProtectedRoute, useAuth } from 'archon-ui';
import IntegrationHubLayout from '../layouts/IntegrationHubLayout';
import Dashboard from '../modules/Dashboard';
import IntegrationsWorkspace from '../modules/Integrations';
import IntegrationWorkspaceDetail from '../modules/Integrations/Detail';
import Integrations from '../modules/Configuration/Integrations';
import IntegrationDetail from '../modules/Configuration/Integrations/Detail';
import IntegrationCategories from '../modules/Configuration/IntegrationCategories';
import Connectors from '../modules/Configuration/Connectors';
import ConnectorDetail from '../modules/Configuration/Connectors/Detail';
import Pipelines from '../modules/Configuration/Pipelines';
import PipelineDetail from '../modules/Configuration/Pipelines/Detail';
import ApiCalls from '../modules/Configuration/ApiCalls';
import JavaScriptFunctions from '../modules/Configuration/JavaScriptFunctions';
import DatabaseScripts from '../modules/Configuration/DatabaseScripts';
import DatabaseConnections from '../modules/Configuration/DatabaseConnections';
import Executions from '../modules/Operations/Executions';
import ProcessingQueue from '../modules/Operations/ProcessingQueue';
import References from '../modules/Operations/References';
import PipelineRoutines from '../modules/Automation/PipelineRoutines';

const identityManagementUrl = import.meta.env.VITE_IDENTITY_PROVIDER_WEB;

function AppRoutes() {
  const { user } = useAuth();

  return (
    <BrowserRouter>
      <Routes>
        <Route
          path="/callback"
          element={
            <Callback
              identityManagementUrl={identityManagementUrl}
              redirectTo="/"
            />
          }
        />

        <Route
          element={
            <ProtectedRoute
              isAuthenticated={!!user}
              redirectTo={identityManagementUrl}
              externalRedirect={true}
            >
              <IntegrationHubLayout />
            </ProtectedRoute>
          }
        >
          <Route index element={<Dashboard />} />
          <Route path="integracoes" element={<IntegrationsWorkspace />} />
          <Route path="integracoes/:id" element={<IntegrationWorkspaceDetail />} />
          <Route path="catalogo/categorias" element={<IntegrationCategories />} />
          <Route path="configuracao-tecnica/integracoes" element={<Integrations />} />
          <Route path="configuracao-tecnica/integracoes/:id" element={<IntegrationDetail />} />
          <Route path="configuracao-tecnica/conectores" element={<Connectors />} />
          <Route path="configuracao-tecnica/conectores/:id" element={<ConnectorDetail />} />
          <Route path="configuracao-tecnica/chamadas-api" element={<ApiCalls />} />
          <Route path="configuracao-tecnica/funcoes-javascript" element={<JavaScriptFunctions />} />
          <Route path="configuracao-tecnica/scripts-banco-dados" element={<DatabaseScripts />} />
          <Route path="configuracao-tecnica/conexoes-banco" element={<DatabaseConnections />} />
          <Route path="configuracao-tecnica/pipelines" element={<Pipelines />} />
          <Route path="configuracao-tecnica/pipelines/:id" element={<PipelineDetail />} />
          <Route path="operacao/execucoes" element={<Executions />} />
          <Route path="operacao/fila" element={<ProcessingQueue />} />
          <Route path="operacao/referencias" element={<References />} />
          <Route path="configuracao-tecnica/automacao" element={<PipelineRoutines />} />
        </Route>

        <Route path="*" element={<div>Página não encontrada</div>} />
      </Routes>
    </BrowserRouter>
  );
}

export default AppRoutes;

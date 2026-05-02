import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Callback, ProtectedRoute, useAuth } from 'archon-ui';
import IntegrationPlataformLayout from '../layouts/IntegrationPlataformLayout';
import Dashboard from '../modules/Dashboard';
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

const identityManagementUrl = import.meta.env.VITE_IDENTITY_MANAGEMENT_URL;
const oidcClientId = import.meta.env.VITE_OIDC_CLIENT_ID || 'integration-plataform-web';

function AppRoutes() {
  const { isAuthenticated } = useAuth();

  return (
    <BrowserRouter>
      <Routes>
        <Route
          path="/callback"
          element={
            <Callback
              identityManagementUrl={identityManagementUrl}
              oidcClientId={oidcClientId}
              redirectTo="/"
            />
          }
        />

        <Route
          element={
            <ProtectedRoute
              isAuthenticated={isAuthenticated}
              redirectTo={identityManagementUrl}
              externalRedirect={true}
              callbackPath="/callback"
              oidcClientId={oidcClientId}
            >
              <IntegrationPlataformLayout />
            </ProtectedRoute>
          }
        >
          <Route index element={<Dashboard />} />
          <Route path="integracoes" element={<Integrations />} />
          <Route path="integracoes/:id" element={<IntegrationDetail />} />
          <Route path="categorias" element={<IntegrationCategories />} />
          <Route path="conectores" element={<Connectors />} />
          <Route path="conectores/:id" element={<ConnectorDetail />} />
          <Route path="chamadas-api" element={<ApiCalls />} />
          <Route path="funcoes-javascript" element={<JavaScriptFunctions />} />
          <Route path="scripts-banco-dados" element={<DatabaseScripts />} />
          <Route path="conexoes-banco" element={<DatabaseConnections />} />
          <Route path="pipelines" element={<Pipelines />} />
          <Route path="pipelines/:id" element={<PipelineDetail />} />
          <Route path="execucoes" element={<Executions />} />
          <Route path="fila" element={<ProcessingQueue />} />
          <Route path="referencias" element={<References />} />
          <Route path="automacao" element={<PipelineRoutines />} />
        </Route>

        <Route path="*" element={<div>Página não encontrada</div>} />
      </Routes>
    </BrowserRouter>
  );
}

export default AppRoutes;

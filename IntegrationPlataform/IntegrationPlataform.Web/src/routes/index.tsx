import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Callback, ProtectedRoute, useAuth } from 'archon-ui';
import IntegrationHubLayout from '../layouts/IntegrationHubLayout';
import Dashboard from '../modules';
import Integracoes from '../modules/configuracao/Integracoes';
import CategoriasIntegracao from '../modules/configuracao/CategoriasIntegracao';
import Conectores from '../modules/configuracao/Conectores';
import Pipelines from '../modules/configuracao/Pipelines';
import PipelineDetalhe from '../modules/configuracao/PipelineDetalhe';
import ConectorDetalhe from '../modules/configuracao/ConectorDetalhe';
import IntegracaoDetalhe from '../modules/configuracao/IntegracaoDetalhe';
import ChamadasApi from '../modules/configuracao/ChamadasApi';
import FuncoesJavaScript from '../modules/configuracao/FuncoesJavaScript';
import ScriptsBancoDados from '../modules/configuracao/ScriptsBancoDados';
import ConexoesBancoDados from '../modules/configuracao/ConexoesBancoDados';
import Execucoes from '../modules/operacional/Execucoes';
import FilaProcessamento from '../modules/operacional/FilaProcessamento';
import Referencias from '../modules/operacional/Referencias';
import Automacao from '../modules/automacao/Automacao';

const identityProviderUrl = import.meta.env.VITE_IDENTITY_PROVIDER_WEB;

function AppRoutes() {
  const { user } = useAuth();

  return (
    <BrowserRouter>
      <Routes>
        <Route
          path="/callback"
          element={
            <Callback
              identityManagementUrl={identityProviderUrl}
              redirectTo="/"
            />
          }
        />

        <Route
          element={
            <ProtectedRoute
              isAuthenticated={!!user}
              redirectTo={identityProviderUrl}
              externalRedirect={true}
            >
              <IntegrationHubLayout />
            </ProtectedRoute>
          }
        >
          <Route index element={<Dashboard />} />
          <Route path="integracoes" element={<Integracoes />} />
          <Route path="integracoes/:id" element={<IntegracaoDetalhe />} />
          <Route path="categorias" element={<CategoriasIntegracao />} />
          <Route path="conectores" element={<Conectores />} />
          <Route path="conectores/:id" element={<ConectorDetalhe />} />
          <Route path="chamadas-api" element={<ChamadasApi />} />
          <Route path="funcoes-javascript" element={<FuncoesJavaScript />} />
          <Route path="scripts-banco-dados" element={<ScriptsBancoDados />} />
          <Route path="conexoes-banco" element={<ConexoesBancoDados />} />
          <Route path="pipelines" element={<Pipelines />} />
          <Route path="pipelines/:id" element={<PipelineDetalhe />} />
          <Route path="execucoes" element={<Execucoes />} />
          <Route path="fila" element={<FilaProcessamento />} />
          <Route path="referencias" element={<Referencias />} />
          <Route path="automacao" element={<Automacao />} />
        </Route>

        <Route path="*" element={<div>Página não encontrada</div>} />
      </Routes>
    </BrowserRouter>
  );
}

export default AppRoutes;

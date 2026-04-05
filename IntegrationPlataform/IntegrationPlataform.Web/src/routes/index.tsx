import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Callback, ProtectedRoute, useAuth } from 'archon-ui';
import IntegrationHubLayout from '../layouts/IntegrationHubLayout';
import Dashboard from '../modules';
import Integracoes from '../modules/Configuracao/Integracoes';
import CategoriasIntegracao from '../modules/Configuracao/CategoriasIntegracao';
import Conectores from '../modules/Configuracao/Conectores';
import Pipelines from '../modules/Configuracao/Pipelines';
import PipelineDetalhe from '../modules/Configuracao/PipelineDetalhe';
import ConectorDetalhe from '../modules/Configuracao/ConectorDetalhe';
import IntegracaoDetalhe from '../modules/Configuracao/IntegracaoDetalhe';
import ChamadasApi from '../modules/Configuracao/ChamadasApi';
import FuncoesJavaScript from '../modules/Configuracao/FuncoesJavaScript';
import ScriptsBancoDados from '../modules/Configuracao/ScriptsBancoDados';
import ConexoesBancoDados from '../modules/Configuracao/ConexoesBancoDados';
import Execucoes from '../modules/Operacional/Execucoes';
import FilaProcessamento from '../modules/Operacional/FilaProcessamento';
import Referencias from '../modules/Operacional/Referencias';
import Automacao from '../modules/Automacao/Automacao';

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

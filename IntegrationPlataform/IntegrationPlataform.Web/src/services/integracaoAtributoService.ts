import { httpClient } from 'archon-ui';
import type { IntegracaoAtributo, CreateIntegracaoAtributoRequest, UpdateIntegracaoAtributoRequest } from '../types/integracaoAtributo';

const BASE_URL = '/IntegrationAttributes';

function mapIntegracaoAtributo(item: any): IntegracaoAtributo {
  return {
    id: item.id,
    campo: item.field,
    label: item.label,
    descricao: item.description,
    placeholder: item.placeholder,
    tipo: item.type,
    valorPadrao: item.defaultValue,
    obrigatorio: item.isRequired,
    ordem: item.order,
    grupo: item.group,
    sensivel: item.isSensitive,
  };
}

export const integracaoAtributoService = {
  getByIntegracao: (integracaoId: number) =>
    httpClient.get<any[]>(`${BASE_URL}/integration/${integracaoId}`).then((response) => ({
      ...response,
      data: response.data?.map(mapIntegracaoAtributo) ?? [],
    })),

  getById: (id: number) =>
    httpClient.get<any>(`${BASE_URL}/${id}`).then((response) => ({
      ...response,
      data: response.data ? mapIntegracaoAtributo(response.data) : response.data,
    })),

  create: (data: CreateIntegracaoAtributoRequest) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/Create`, {
      integrationId: data.integracaoId,
      field: data.campo,
      label: data.label,
      description: data.descricao,
      placeholder: data.placeholder,
      type: data.tipo,
      defaultValue: data.valorPadrao,
      isRequired: data.obrigatorio,
      order: data.ordem,
      group: data.grupo,
      isSensitive: data.sensivel,
    }),

  update: (id: number, data: UpdateIntegracaoAtributoRequest) =>
    httpClient.put<{ message: string }>(`${BASE_URL}/${id}`, {
      id,
      field: data.campo,
      label: data.label,
      description: data.descricao,
      placeholder: data.placeholder,
      type: data.tipo,
      defaultValue: data.valorPadrao,
      isRequired: data.obrigatorio,
      order: data.ordem,
      group: data.grupo,
      isSensitive: data.sensivel,
    }),

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};

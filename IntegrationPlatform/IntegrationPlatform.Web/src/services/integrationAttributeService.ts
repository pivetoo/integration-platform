import { httpClient } from 'archon-ui';
import type { IntegracaoAtributo, CreateIntegracaoAtributoRequest, UpdateIntegracaoAtributoRequest } from '../types/integrationAttribute';

const BASE_URL = '/IntegrationAttributes';

export const integrationAttributeService = {
  getByIntegration: (integrationId: number) =>
    httpClient.get<IntegracaoAtributo[]>(`${BASE_URL}/integration/${integrationId}?includeHidden=true`),

  getById: (id: number) =>
    httpClient.get<IntegracaoAtributo>(`${BASE_URL}/GetById/${id}`),

  create: (data: CreateIntegracaoAtributoRequest) =>
    httpClient.post<IntegracaoAtributo>(`${BASE_URL}/Create`, {
      integrationId: data.integrationId,
      field: data.field,
      label: data.label,
      description: data.description,
      placeholder: data.placeholder,
      type: data.type,
      defaultValue: data.defaultValue,
      isRequired: data.isRequired,
      order: data.order,
      group: data.group,
      isSensitive: data.isSensitive,
      isHidden: data.isHidden,
    }),

  update: (id: number, data: UpdateIntegracaoAtributoRequest) =>
    httpClient.put<IntegracaoAtributo>(`${BASE_URL}/Update/${id}`, {
      id,
      field: data.field,
      label: data.label,
      description: data.description,
      placeholder: data.placeholder,
      type: data.type,
      defaultValue: data.defaultValue,
      isRequired: data.isRequired,
      order: data.order,
      group: data.group,
      isSensitive: data.isSensitive,
      isHidden: data.isHidden,
    }),

  delete: (id: number) =>
    httpClient.delete<IntegracaoAtributo>(`${BASE_URL}/Delete/${id}`),
};

import { httpClient } from 'd-rts';
import type { IntegracaoAtributo, CreateIntegracaoAtributoRequest, UpdateIntegracaoAtributoRequest } from '../types/integracaoAtributo';

const BASE_URL = '/integracaoatributo';

export const integracaoAtributoService = {
  getByIntegracao: (integracaoId: number) =>
    httpClient.get<IntegracaoAtributo[]>(`${BASE_URL}/integracao/${integracaoId}`),

  getById: (id: number) =>
    httpClient.get<IntegracaoAtributo>(`${BASE_URL}/${id}`),

  create: (data: CreateIntegracaoAtributoRequest) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/Create`, data),

  update: (id: number, data: UpdateIntegracaoAtributoRequest) =>
    httpClient.put<{ message: string }>(`${BASE_URL}/${id}`, data),

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};

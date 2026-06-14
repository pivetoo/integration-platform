import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { ServiceContract, CreateServiceContractRequest, UpdateServiceContractRequest } from '../types/serviceContract';

const BASE_URL = '/ServiceContracts';

export const serviceContractService = {
  getAll: async (params?: PaginationParams & { integrationCategoryId?: number }): Promise<PaginatedResult<ServiceContract>> => {
    const query = buildPaginationQuery(params);
    const extra: string[] = [];
    if (params?.search) {
      extra.push(`search=${encodeURIComponent(params.search)}`);
    }
    if (params?.integrationCategoryId) {
      extra.push(`integrationCategoryId=${params.integrationCategoryId}`);
    }
    const extraQuery = extra.length > 0 ? `${query ? '&' : '?'}${extra.join('&')}` : '';
    const response = await httpClient.get<ServiceContract[]>(`${BASE_URL}/Get${query}${extraQuery}`);

    return {
      data: response.data ?? [],
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getActive: async () => {
    const response = await httpClient.get<ServiceContract[]>(`${BASE_URL}/active`);
    return response.data ?? [];
  },

  getById: async (id: number) => httpClient.get<ServiceContract>(`${BASE_URL}/GetById/${id}`),

  getByIdentifier: async (identifier: string) =>
    httpClient.get<ServiceContract>(`${BASE_URL}/by-identifier/${encodeURIComponent(identifier)}`),

  create: async (data: CreateServiceContractRequest) =>
    httpClient.post<ServiceContract>(`${BASE_URL}/Create`, {
      identifier: data.identifier,
      name: data.name,
      description: data.description || undefined,
      integrationCategoryId: data.integrationCategoryId,
      inputSchema: data.inputSchema || undefined,
      outputSchema: data.outputSchema || undefined,
      hasCallback: data.hasCallback,
      callbackSchema: data.callbackSchema || undefined,
    }),

  update: async (id: number, data: UpdateServiceContractRequest) =>
    httpClient.put<ServiceContract>(`${BASE_URL}/Update/${id}`, {
      id,
      identifier: data.identifier,
      name: data.name,
      description: data.description || undefined,
      integrationCategoryId: data.integrationCategoryId,
      inputSchema: data.inputSchema || undefined,
      outputSchema: data.outputSchema || undefined,
      hasCallback: data.hasCallback,
      callbackSchema: data.callbackSchema || undefined,
      isActive: data.isActive,
    }),

  delete: (id: number) => httpClient.delete<{ message: string }>(`${BASE_URL}/Delete/${id}`),

  setIntegrationBinding: (integrationId: number, serviceContractId: number, isActive = true) =>
    httpClient.post(`${BASE_URL}/integration-binding`, { integrationId, serviceContractId, isActive }),

  removeIntegrationBinding: (integrationId: number, serviceContractId: number) =>
    httpClient.delete(`${BASE_URL}/integration-binding/${integrationId}/${serviceContractId}`),
};

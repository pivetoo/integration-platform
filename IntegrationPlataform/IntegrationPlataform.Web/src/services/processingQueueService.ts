import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { ProcessingQueueItem, CreateProcessingQueueItemRequest } from '../types/processingQueue';

const BASE_URL = '/ProcessingQueues';

export const processingQueueService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<ProcessingQueueItem>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<ProcessingQueueItem[]>(`${BASE_URL}/Get${query}`);
    return {
      data: response.data ?? [],
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getById: async (id: number) => httpClient.get<ProcessingQueueItem>(`${BASE_URL}/${id}`),

  getPending: async () => httpClient.get<ProcessingQueueItem[]>(`${BASE_URL}/pending`),

  create: (data: CreateProcessingQueueItemRequest) =>
    httpClient.post<ProcessingQueueItem>(`${BASE_URL}/enqueue`, {
      connectorId: data.connectorId,
      pipelineId: data.pipelineId,
      payload: data.payload,
      priority: data.priority,
    }),

  delete: (id: number) =>
    httpClient.delete<ProcessingQueueItem>(`${BASE_URL}/${id}`),
};

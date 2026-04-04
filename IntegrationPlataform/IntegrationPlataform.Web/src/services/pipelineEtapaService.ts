import { httpClient } from 'd-rts';
import type { PipelineEtapa, CreatePipelineEtapaRequest, UpdatePipelineEtapaRequest } from '../types/pipeline';

const BASE_URL = '/pipelineetapa';

export const pipelineEtapaService = {
  getByPipeline: (pipelineId: number) =>
    httpClient.get<PipelineEtapa[]>(`${BASE_URL}/pipeline/${pipelineId}`),

  getById: (id: number) => httpClient.get<PipelineEtapa>(`${BASE_URL}/${id}`),

  create: (data: CreatePipelineEtapaRequest) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/Create`, data),

  update: (id: number, data: UpdatePipelineEtapaRequest) =>
    httpClient.put<{ message: string }>(`${BASE_URL}/${id}`, data),

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),

  reorder: (etapas: Array<{ id: number; ordem: number }>) =>
    httpClient.post<{ message: string }>(`${BASE_URL}/Reorder`, { etapas }),
};

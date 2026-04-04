import { httpClient } from 'archon-ui';
import type { ExecucaoLog } from '../types/execucaoLog';

const BASE_URL = '/execucaolog';

export const execucaoLogService = {
  getByExecucao: (execucaoId: number) =>
    httpClient.get<ExecucaoLog[]>(`${BASE_URL}/execucao/${execucaoId}`),
};

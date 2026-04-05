import { httpClient } from 'archon-ui';
import type { ExecutionLog } from '../types/execucaoLog';

const BASE_URL = '/ExecutionLogs';

export const execucaoLogService = {
  getByExecution: async (executionId: number) => {
    return httpClient.get<ExecutionLog[]>(`${BASE_URL}/execution/${executionId}`);
  },
};

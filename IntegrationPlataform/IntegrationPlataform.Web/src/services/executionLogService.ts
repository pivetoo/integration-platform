import { httpClient } from 'archon-ui';
import type { ExecutionLog } from '../types/executionLog';

const BASE_URL = '/ExecutionLogs';

export const executionLogService = {
  getByExecution: async (executionId: number) => {
    return httpClient.get<ExecutionLog[]>(`${BASE_URL}/execution/${executionId}`);
  },
};

import { httpClient } from 'd-rts';
import type { CopilotPlanRequest, CopilotPlanResponse } from '../types/copilot';

const BASE_URL = '/copilot';

export const copilotService = {
  plan: async (data: CopilotPlanRequest): Promise<CopilotPlanResponse> => {
    const response = await httpClient.post<CopilotPlanResponse>(`${BASE_URL}/plan`, data);
    if (!response.data) {
      throw new Error('Resposta vazia ao gerar plano.')
    }

    return response.data;
  },
};

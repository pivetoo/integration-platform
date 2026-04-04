import { httpClient } from 'd-rts';
import type { ConectorAtributoValor, CreateConectorAtributoValorRequest, UpdateConectorAtributoValorRequest } from '../types/conectorAtributoValor';

const BASE_URL = '/conectoratributovalor';

export const conectorAtributoValorService = {
  getByConector: (conectorId: number) =>
    httpClient.get<ConectorAtributoValor[]>(`${BASE_URL}/conector/${conectorId}`),

  create: (data: CreateConectorAtributoValorRequest) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/Create`, data),

  update: (id: number, data: UpdateConectorAtributoValorRequest) =>
    httpClient.put<{ message: string }>(`${BASE_URL}/${id}`, data),

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};

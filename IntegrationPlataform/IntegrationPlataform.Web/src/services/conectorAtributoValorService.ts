import { httpClient } from 'archon-ui';
import type { ConnectorAttributeValue, CreateConnectorAttributeValueRequest, UpdateConnectorAttributeValueRequest } from '../types/conectorAtributoValor';

const BASE_URL = '/ConnectorAttributeValues';

export const conectorAtributoValorService = {
  getByConnector: (connectorId: number) =>
    httpClient.get<ConnectorAttributeValue[]>(`${BASE_URL}/connector/${connectorId}`),

  create: (data: CreateConnectorAttributeValueRequest) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/Create`, {
      connectorId: data.connectorId,
      integrationAttributeId: data.integrationAttributeId,
      value: data.value,
    }),

  update: (id: number, data: UpdateConnectorAttributeValueRequest) =>
    httpClient.put<{ message: string }>(`${BASE_URL}/${id}`, {
      id,
      integrationAttributeId: data.integrationAttributeId,
      value: data.value,
    }),

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};

import { httpClient } from 'archon-ui';
import type { ConectorAtributoValor, CreateConectorAtributoValorRequest, UpdateConectorAtributoValorRequest } from '../types/conectorAtributoValor';

const BASE_URL = '/ConnectorAttributeValues';

function mapConectorAtributoValor(item: any): ConectorAtributoValor {
  return {
    id: item.id,
    conectorId: item.connectorId,
    conector: item.connector ?? null,
    integracaoAtributoId: item.integrationAttributeId,
    integracaoAtributo: item.integrationAttribute
      ? {
          id: item.integrationAttribute.id,
          campo: item.integrationAttribute.field,
          label: item.integrationAttribute.label,
          descricao: item.integrationAttribute.description,
          placeholder: item.integrationAttribute.placeholder,
          tipo: item.integrationAttribute.type,
          valorPadrao: item.integrationAttribute.defaultValue,
          obrigatorio: item.integrationAttribute.isRequired,
          ordem: item.integrationAttribute.order,
          grupo: item.integrationAttribute.group,
          sensivel: item.integrationAttribute.isSensitive,
        }
      : null,
    valor: item.value,
  };
}

export const conectorAtributoValorService = {
  getByConector: (conectorId: number) =>
    httpClient.get<any[]>(`${BASE_URL}/connector/${conectorId}`).then((response) => ({
      ...response,
      data: response.data?.map(mapConectorAtributoValor) ?? [],
    })),

  create: (data: CreateConectorAtributoValorRequest) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/Create`, {
      connectorId: data.conectorId,
      integrationAttributeId: data.integracaoAtributoId,
      value: data.valor,
    }),

  update: (id: number, data: UpdateConectorAtributoValorRequest) =>
    httpClient.put<{ message: string }>(`${BASE_URL}/${id}`, {
      id,
      integrationAttributeId: data.integracaoAtributoId,
      value: data.valor,
    }),

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};

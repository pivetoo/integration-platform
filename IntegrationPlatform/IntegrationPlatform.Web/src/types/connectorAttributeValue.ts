import type { Conector } from './connector';
import type { IntegracaoAtributo } from './integrationAttribute';

export interface ConnectorAttributeValue {
  id: number;
  connectorId: number;
  connector: Conector | null;
  integrationAttributeId: number;
  integrationAttribute: IntegracaoAtributo | null;
  value: string;
}

export interface CreateConnectorAttributeValueRequest {
  connectorId: number;
  integrationAttributeId: number;
  value: string;
}

export interface UpdateConnectorAttributeValueRequest {
  integrationAttributeId: number;
  value: string;
}

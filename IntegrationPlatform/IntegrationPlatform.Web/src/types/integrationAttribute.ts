export const TipoCampo = {
  Texto: 1,
  TextoLongo: 2,
  Numero: 3,
  Decimal: 4,
  Booleano: 5,
  Data: 6,
  DataHora: 7,
  Lista: 8,
} as const;

export type TipoCampo = typeof TipoCampo[keyof typeof TipoCampo];

export const TipoCampoLabels: Record<TipoCampo, string> = {
  [TipoCampo.Texto]: 'Texto',
  [TipoCampo.TextoLongo]: 'Texto Longo',
  [TipoCampo.Numero]: 'Número',
  [TipoCampo.Decimal]: 'Decimal',
  [TipoCampo.Booleano]: 'Booleano',
  [TipoCampo.Data]: 'Data',
  [TipoCampo.DataHora]: 'Data/Hora',
  [TipoCampo.Lista]: 'Lista',
};

export interface IntegracaoAtributo {
  id: number;
  integrationId?: number;
  field: string;
  label: string;
  description?: string;
  placeholder?: string;
  type: TipoCampo;
  defaultValue?: string;
  isRequired: boolean;
  order: number;
  group?: string;
  isSensitive: boolean;
  isHidden: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateIntegracaoAtributoRequest {
  integrationId: number;
  field: string;
  label: string;
  description?: string;
  placeholder?: string;
  type: TipoCampo;
  defaultValue?: string;
  isRequired: boolean;
  order: number;
  group?: string;
  isSensitive: boolean;
  isHidden: boolean;
}

export interface UpdateIntegracaoAtributoRequest {
  field: string;
  label: string;
  description?: string;
  placeholder?: string;
  type: TipoCampo;
  defaultValue?: string;
  isRequired: boolean;
  order: number;
  group?: string;
  isSensitive: boolean;
  isHidden: boolean;
}

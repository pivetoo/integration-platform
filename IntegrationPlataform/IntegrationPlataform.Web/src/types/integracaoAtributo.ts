export const TipoCampo = {
  Texto: 0,
  TextoLongo: 1,
  Numero: 2,
  Decimal: 3,
  Booleano: 4,
  Data: 5,
  DataHora: 6,
  Lista: 7,
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
  campo: string;
  label: string;
  descricao?: string;
  placeholder?: string;
  tipo: TipoCampo;
  valorPadrao?: string;
  obrigatorio: boolean;
  ordem: number;
  grupo?: string;
  sensivel: boolean;
}

export interface CreateIntegracaoAtributoRequest {
  integracaoId: number;
  campo: string;
  label: string;
  descricao?: string;
  placeholder?: string;
  tipo: TipoCampo;
  valorPadrao?: string;
  obrigatorio: boolean;
  ordem: number;
  grupo?: string;
  sensivel: boolean;
}

export interface UpdateIntegracaoAtributoRequest {
  campo: string;
  label: string;
  descricao?: string;
  placeholder?: string;
  tipo: TipoCampo;
  valorPadrao?: string;
  obrigatorio: boolean;
  ordem: number;
  grupo?: string;
  sensivel: boolean;
}

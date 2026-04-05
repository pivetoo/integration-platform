import type { Conector } from './conector';
import type { IntegracaoAtributo } from './integracaoAtributo';

export interface ConectorAtributoValor {
  id: number;
  conectorId: number;
  conector: Conector | null;
  integracaoAtributoId: number;
  integracaoAtributo: IntegracaoAtributo | null;
  valor: string;
}

export interface CreateConectorAtributoValorRequest {
  conectorId: number;
  integracaoAtributoId: number;
  valor: string;
}

export interface UpdateConectorAtributoValorRequest {
  integracaoAtributoId: number;
  valor: string;
}

import type { Conector } from './conector';
import type { IntegracaoAtributo } from './integracaoAtributo';

export interface ConectorAtributoValor {
  id: number;
  conector: Conector;
  integracaoAtributo: IntegracaoAtributo;
  valor: string;
}

export interface CreateConectorAtributoValorRequest {
  conectorId: number;
  integracaoAtributoId: number;
  valor: string;
}

export interface UpdateConectorAtributoValorRequest {
  valor: string;
}

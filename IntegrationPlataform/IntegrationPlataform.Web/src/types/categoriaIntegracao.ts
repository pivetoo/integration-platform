export interface CategoriaIntegracao {
  id: number;
  nome: string;
  descricao: string | null;
  ativo: boolean;
  criadoEm: string;
  ultimaAlteracao: string;
}

export interface CreateCategoriaIntegracaoRequest {
  nome: string;
  descricao: string;
}

export interface UpdateCategoriaIntegracaoRequest {
  nome: string;
  descricao: string;
  ativo: boolean;
}

export interface FuncaoJavaScript {
  id: number;
  nome: string;
  descricao: string | null;
  codigo: string;
  criadoEm: string;
  ultimaAlteracao: string;
}

export interface CreateFuncaoJavaScriptRequest {
  nome: string;
  descricao: string;
  codigo: string;
}

export interface UpdateFuncaoJavaScriptRequest {
  nome: string;
  descricao: string;
  codigo: string;
}

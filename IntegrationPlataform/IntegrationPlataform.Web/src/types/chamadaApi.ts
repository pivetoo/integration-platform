export const MetodoHttp = {
  GET: 0,
  POST: 1,
  PUT: 2,
  PATCH: 3,
  DELETE: 4,
} as const;

export type MetodoHttp = typeof MetodoHttp[keyof typeof MetodoHttp];

export const MetodoHttpLabels: Record<MetodoHttp, string> = {
  [MetodoHttp.GET]: 'GET',
  [MetodoHttp.POST]: 'POST',
  [MetodoHttp.PUT]: 'PUT',
  [MetodoHttp.PATCH]: 'PATCH',
  [MetodoHttp.DELETE]: 'DELETE',
};

export interface ChamadaApi {
  id: number;
  nome: string;
  descricao?: string;
  metodo: MetodoHttp;
  url: string;
  headersTemplate?: string;
  bodyTemplate?: string;
}

export interface CreateChamadaApiRequest {
  nome: string;
  descricao?: string;
  metodo: MetodoHttp;
  url: string;
  headersTemplate?: string;
  bodyTemplate?: string;
}

export interface UpdateChamadaApiRequest {
  nome: string;
  descricao?: string;
  metodo: MetodoHttp;
  url: string;
  headersTemplate?: string;
  bodyTemplate?: string;
}

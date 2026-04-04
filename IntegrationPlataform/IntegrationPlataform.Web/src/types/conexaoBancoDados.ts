export const TipoBancoDados = {
  PostgreSQL: 0,
  SqlServer: 1,
  Oracle: 2,
  MySQL: 3,
} as const;

export type TipoBancoDados = typeof TipoBancoDados[keyof typeof TipoBancoDados];

export const TipoBancoDadosLabels: Record<TipoBancoDados, string> = {
  [TipoBancoDados.PostgreSQL]: 'PostgreSQL',
  [TipoBancoDados.SqlServer]: 'SQL Server',
  [TipoBancoDados.Oracle]: 'Oracle',
  [TipoBancoDados.MySQL]: 'MySQL',
};

export interface ConexaoBancoDados {
  id: number;
  nome: string;
  tipo: TipoBancoDados;
  host: string;
  port: number;
  database: string;
  username: string;
  password: string;
  criadoEm: string;
  ultimaAlteracao?: string;
}

export interface CreateConexaoBancoDadosRequest {
  nome: string;
  tipo: TipoBancoDados;
  host: string;
  port: number;
  database: string;
  username: string;
  password: string;
}

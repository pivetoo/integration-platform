export const TipoBancoDados = {
  PostgreSQL: 1,
  SqlServer: 2,
  Oracle: 3,
  MySQL: 4,
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

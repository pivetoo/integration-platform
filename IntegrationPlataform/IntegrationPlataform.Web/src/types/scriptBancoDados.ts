import type { DatabaseConnection } from './conexaoBancoDados';

export interface DatabaseScript {
  id: number;
  databaseConnectionId: number;
  databaseConnection?: DatabaseConnection;
  name: string;
  description: string | null;
  script: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateDatabaseScriptRequest {
  databaseConnectionId: number;
  name: string;
  description: string;
  script: string;
}

export interface UpdateDatabaseScriptRequest {
  databaseConnectionId: number;
  name: string;
  description: string;
  script: string;
}

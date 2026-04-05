export const DatabaseType = {
  PostgreSQL: 1,
  SqlServer: 2,
  Oracle: 3,
  MySQL: 4,
} as const;

export type DatabaseType = typeof DatabaseType[keyof typeof DatabaseType];

export const DatabaseTypeLabels: Record<DatabaseType, string> = {
  [DatabaseType.PostgreSQL]: 'PostgreSQL',
  [DatabaseType.SqlServer]: 'SQL Server',
  [DatabaseType.Oracle]: 'Oracle',
  [DatabaseType.MySQL]: 'MySQL',
};

export interface DatabaseConnection {
  id: number;
  name: string;
  type: DatabaseType;
  host: string;
  port: number;
  database: string;
  username: string;
  password: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateDatabaseConnectionRequest {
  name: string;
  type: DatabaseType;
  host: string;
  port: number;
  database: string;
  username: string;
  password: string;
}

export interface UpdateDatabaseConnectionRequest {
  name: string;
  type: DatabaseType;
  host: string;
  port: number;
  database: string;
  username: string;
  password: string;
}

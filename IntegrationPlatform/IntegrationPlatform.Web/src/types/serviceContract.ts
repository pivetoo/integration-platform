export interface ServiceContract {
  id: number;
  identifier: string;
  name: string;
  description: string | null;
  integrationCategoryId: number;
  inputSchema: string | null;
  outputSchema: string | null;
  hasCallback: boolean;
  callbackSchema: string | null;
  includeOutputInCallback: boolean;
  isActive: boolean;
  isSystem: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateServiceContractRequest {
  identifier: string;
  name: string;
  description?: string;
  integrationCategoryId: number;
  inputSchema?: string;
  outputSchema?: string;
  hasCallback: boolean;
  callbackSchema?: string;
}

export interface UpdateServiceContractRequest {
  identifier: string;
  name: string;
  description?: string;
  integrationCategoryId: number;
  inputSchema?: string;
  outputSchema?: string;
  hasCallback: boolean;
  callbackSchema?: string;
  isActive: boolean;
}

export interface IntegrationCategory {
  id: number;
  identifier: string;
  name: string;
  description: string | null;
  isActive: boolean;
  isSystem: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateIntegrationCategoryRequest {
  identifier: string;
  name: string;
  description: string;
}

export interface UpdateIntegrationCategoryRequest {
  identifier: string;
  name: string;
  description: string;
  isActive: boolean;
}

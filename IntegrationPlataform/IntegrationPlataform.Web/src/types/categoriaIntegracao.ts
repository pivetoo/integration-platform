export interface IntegrationCategory {
  id: number;
  name: string;
  description: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateIntegrationCategoryRequest {
  name: string;
  description: string;
}

export interface UpdateIntegrationCategoryRequest {
  name: string;
  description: string;
  isActive: boolean;
}

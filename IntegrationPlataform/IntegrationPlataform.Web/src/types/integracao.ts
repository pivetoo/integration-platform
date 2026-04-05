import type { IntegrationCategory } from './categoriaIntegracao';

export interface Integration {
  id: number;
  identifier: string;
  name: string;
  description?: string;
  integrationCategoryId?: number;
  integrationCategory?: IntegrationCategory;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateIntegrationRequest {
  identifier: string;
  name: string;
  description?: string;
  integrationCategoryId?: number;
  isActive: boolean;
}

export interface UpdateIntegrationRequest {
  identifier: string;
  name: string;
  description?: string;
  integrationCategoryId?: number;
  isActive: boolean;
}

export interface IntegrationExportModel {
  identifier: string;
  name: string;
  description?: string;
  attributes: IntegrationAttributeExportModel[];
  apiCalls: ApiCallExportModel[];
  javaScriptFunctions: JavaScriptFunctionExportModel[];
  pipelines: PipelineExportModel[];
}

export interface IntegrationAttributeExportModel {
  field: string;
  label: string;
  description?: string;
  placeholder?: string;
  type: number;
  defaultValue?: string;
  isRequired: boolean;
  order: number;
  group?: string;
  isSensitive: boolean;
}

export interface ApiCallExportModel {
  name: string;
  description?: string;
  method: number;
  url: string;
  headersTemplate?: string;
  bodyTemplate?: string;
}

export interface JavaScriptFunctionExportModel {
  name: string;
  description?: string;
  code: string;
}

export interface PipelineExportModel {
  identifier: string;
  name: string;
  description?: string;
  trigger: number;
  isActive: boolean;
  steps: PipelineStepExportModel[];
}

export interface PipelineStepExportModel {
  order: number;
  name: string;
  type: number;
  apiCallName?: string;
  javaScriptFunctionName?: string;
  errorAction: number;
  isActive: boolean;
}

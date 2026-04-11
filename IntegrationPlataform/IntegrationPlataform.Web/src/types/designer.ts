export interface FieldSuggestion {
  key: string
  label: string
  source: string
  sourceName?: string
  type: string
  exampleValue?: string
  pipelineStepId?: number
  pipelineExampleId?: number
}

export interface ApiCallPreviewResult {
  method: string
  url: string
  headers: Record<string, string>
  body?: string
}

export interface PipelineStepPreviewResult {
  pipelineStepId: number
  stepName: string
  stepType: string
  url?: string
  headers: Record<string, string>
  body?: string
  requestExample?: string
  responseExample?: string
}

export interface GetFieldSuggestionsRequest {
  connectorId?: number
  pipelineExampleId?: number
  pipelineStepExampleId?: number
  payloadExample?: string
}

export interface GenerateApiCallPreviewRequest {
  apiCallId: number
  connectorId?: number
  pipelineExampleId?: number
  pipelineStepExampleId?: number
  payloadExample?: string
}

export interface GeneratePipelineStepPreviewRequest {
  pipelineStepId: number
  connectorId?: number
  pipelineExampleId?: number
  payloadExample?: string
}

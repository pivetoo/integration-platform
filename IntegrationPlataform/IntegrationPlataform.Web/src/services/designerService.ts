import { httpClient } from 'archon-ui'
import type {
  ApiCallPreviewResult,
  FieldSuggestion,
  GenerateApiCallPreviewRequest,
  GeneratePipelineStepPreviewRequest,
  GetFieldSuggestionsRequest,
  PipelineStepPreviewResult,
} from '../types/designer'

const BASE_URL = '/Designer'

function buildQuery(params: GetFieldSuggestionsRequest) {
  const search = new URLSearchParams()

  if (params.connectorId) search.set('connectorId', String(params.connectorId))
  if (params.pipelineExampleId) search.set('pipelineExampleId', String(params.pipelineExampleId))
  if (params.pipelineStepExampleId) search.set('pipelineStepExampleId', String(params.pipelineStepExampleId))
  if (params.payloadExample) search.set('payloadExample', params.payloadExample)

  const query = search.toString()
  return query ? `?${query}` : ''
}

export const designerService = {
  getFieldSuggestions: async (params: GetFieldSuggestionsRequest) => {
    const response = await httpClient.get<FieldSuggestion[]>(`${BASE_URL}/field-suggestions${buildQuery(params)}`)
    return response.data ?? []
  },

  generateApiCallPreview: async (payload: GenerateApiCallPreviewRequest) => {
    return httpClient.post<ApiCallPreviewResult>(`${BASE_URL}/api-call-preview`, payload)
  },

  generatePipelineStepPreview: async (payload: GeneratePipelineStepPreviewRequest) => {
    return httpClient.post<PipelineStepPreviewResult>(`${BASE_URL}/pipeline-step-preview`, payload)
  },
}

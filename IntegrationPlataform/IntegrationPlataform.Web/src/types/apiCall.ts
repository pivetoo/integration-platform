export const HttpMethod = {
  GET: 1,
  POST: 2,
  PUT: 3,
  PATCH: 4,
  DELETE: 5,
} as const;

export type HttpMethod = typeof HttpMethod[keyof typeof HttpMethod];

export const HttpMethodLabels: Record<HttpMethod, string> = {
  [HttpMethod.GET]: 'GET',
  [HttpMethod.POST]: 'POST',
  [HttpMethod.PUT]: 'PUT',
  [HttpMethod.PATCH]: 'PATCH',
  [HttpMethod.DELETE]: 'DELETE',
};

export interface ApiCall {
  id: number;
  name: string;
  description?: string;
  method: HttpMethod;
  url: string;
  headersTemplate?: string;
  bodyTemplate?: string;
}

export interface CreateApiCallRequest {
  name: string;
  description?: string;
  method: HttpMethod;
  url: string;
  headersTemplate?: string;
  bodyTemplate?: string;
}

export interface UpdateApiCallRequest {
  name: string;
  description?: string;
  method: HttpMethod;
  url: string;
  headersTemplate?: string;
  bodyTemplate?: string;
}

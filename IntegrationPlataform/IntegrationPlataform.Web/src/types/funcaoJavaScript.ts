export interface JavaScriptFunction {
  id: number;
  name: string;
  description: string | null;
  code: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateJavaScriptFunctionRequest {
  name: string;
  description: string;
  code: string;
}

export interface UpdateJavaScriptFunctionRequest {
  name: string;
  description: string;
  code: string;
}

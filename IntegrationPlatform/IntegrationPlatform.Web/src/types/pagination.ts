export interface PaginationParams {
  page?: number
  pageSize?: number
  search?: string
  orderBy?: string
}

export interface PaginatedResult<T> {
  data: T[]
  total: number
  page?: number
  pageSize?: number
}

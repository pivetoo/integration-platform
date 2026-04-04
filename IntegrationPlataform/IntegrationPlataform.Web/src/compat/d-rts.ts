export * from 'archon-ui'

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

export interface ODataParams {
  $top?: number
  $skip?: number
  $count?: boolean
  $filter?: string
  $orderby?: string
  $select?: string
  $expand?: string
}

export interface ODataResponse<T> {
  value?: T[]
  '@odata.count'?: number
  '@odata.nextLink'?: string
  d?: {
    __count?: number | string
    results?: T[]
  }
}

export { setIdentityManagementURL as setIdentityProviderURL } from 'archon-ui'

function escapeODataValue(value: string): string {
  return value.replace(/'/g, "''")
}

export class ODataHelper {
  static buildQuery(params: ODataParams): string {
    const queryParts: string[] = []

    if (params.$top !== undefined) {
      queryParts.push(`$top=${params.$top}`)
    }

    if (params.$skip !== undefined) {
      queryParts.push(`$skip=${params.$skip}`)
    }

    if (params.$count !== undefined) {
      queryParts.push(`$count=${params.$count}`)
    }

    if (params.$filter) {
      queryParts.push(`$filter=${encodeURIComponent(params.$filter)}`)
    }

    if (params.$orderby) {
      queryParts.push(`$orderby=${encodeURIComponent(params.$orderby)}`)
    }

    if (params.$select) {
      queryParts.push(`$select=${params.$select}`)
    }

    if (params.$expand) {
      queryParts.push(`$expand=${params.$expand}`)
    }

    return queryParts.length > 0 ? `?${queryParts.join('&')}` : ''
  }

  static fromPaginationParams(params: PaginationParams): ODataParams {
    const oDataParams: ODataParams = {
      $count: true,
    }

    if (params.pageSize) {
      oDataParams.$top = params.pageSize
    }

    if (params.page && params.pageSize) {
      oDataParams.$skip = (params.page - 1) * params.pageSize
    }

    if (params.orderBy) {
      oDataParams.$orderby = params.orderBy
    }

    if (params.search) {
      const normalizedSearch = escapeODataValue(params.search.toLowerCase())
      oDataParams.$filter = `contains(tolower(nome), '${normalizedSearch}')`
    }

    return oDataParams
  }

  static createSearchFilter(searchTerm: string, fields: string[]): string {
    if (!searchTerm || fields.length === 0) {
      return ''
    }

    const normalizedSearch = escapeODataValue(searchTerm.toLowerCase())

    return fields
      .map((field) => `contains(tolower(${field}), '${normalizedSearch}')`)
      .join(' or ')
  }

  static createBooleanFilter(field: string, value: boolean): string {
    return `${field} eq ${value}`
  }

  static createDateFilter(
    field: string,
    operator: 'eq' | 'gt' | 'lt' | 'ge' | 'le',
    date: Date,
  ): string {
    return `${field} ${operator} ${date.toISOString()}`
  }

  static combineFilters(filters: string[], operator: 'and' | 'or' = 'and'): string {
    const validFilters = filters.filter((filter) => filter.trim().length > 0)

    if (validFilters.length === 0) {
      return ''
    }

    if (validFilters.length === 1) {
      return validFilters[0]
    }

    return validFilters.map((filter) => `(${filter})`).join(` ${operator} `)
  }

  static extractData<T>(response: ODataResponse<T> | T[]): T[] {
    if (Array.isArray(response)) {
      return response
    }

    if (Array.isArray(response.value)) {
      return response.value
    }

    if (Array.isArray(response.d?.results)) {
      return response.d.results
    }

    return []
  }

  static extractCount<T>(response: ODataResponse<T> | T[]): number {
    if (Array.isArray(response)) {
      return response.length
    }

    if (typeof response['@odata.count'] === 'number') {
      return response['@odata.count']
    }

    if (typeof response.d?.__count === 'number') {
      return response.d.__count
    }

    if (typeof response.d?.__count === 'string') {
      const parsedCount = Number(response.d.__count)
      return Number.isNaN(parsedCount) ? 0 : parsedCount
    }

    return this.extractData(response).length
  }

  static processResponse<T>(
    response: ODataResponse<T> | T[] | undefined,
    params?: {
      page?: number
      pageSize?: number
    },
    mapFn?: (item: unknown) => T,
  ): PaginatedResult<T> {
    const normalizedResponse = response ?? []
    const data = this.extractData(normalizedResponse)
    const mappedData = mapFn ? data.map((item) => mapFn(item)) : data

    return {
      data: mappedData,
      total: this.extractCount(normalizedResponse),
      page: params?.page,
      pageSize: params?.pageSize,
    }
  }
}

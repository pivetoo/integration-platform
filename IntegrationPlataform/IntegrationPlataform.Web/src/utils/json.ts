export function parseJsonSafe<T = unknown>(value: string): T | null {
  try {
    return JSON.parse(value) as T;
  } catch {
    return null;
  }
}

export function normalizeJsonString(value: string): string {
  const parsed = parseJsonSafe(value);
  return parsed === null ? value : JSON.stringify(parsed);
}

export function prettyJson(value: unknown): string {
  if (value == null) {
    return 'null';
  }

  if (typeof value === 'string') {
    const parsed = parseJsonSafe(value);
    return parsed === null ? value : JSON.stringify(parsed, null, 2);
  }

  return JSON.stringify(value, null, 2);
}

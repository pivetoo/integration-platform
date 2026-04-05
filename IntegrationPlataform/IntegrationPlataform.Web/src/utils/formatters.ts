export function formatDateTime(dateStr?: string): string {
  if (!dateStr) {
    return '-';
  }

  return new Date(dateStr).toLocaleString('pt-BR');
}

export function formatDuration(ms?: number): string {
  if (ms == null) {
    return '-';
  }

  if (ms < 1000) {
    return `${ms}ms`;
  }

  const seconds = Math.floor(ms / 1000);
  if (seconds < 60) {
    return `${seconds}s`;
  }

  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  return `${minutes}m ${remainingSeconds}s`;
}

export function formatTime(dateStr?: string): string {
  if (!dateStr) {
    return '-';
  }

  return new Date(dateStr).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
}

namespace IntegrationPlatform.Domain.ValueObjects
{
    // Calculo do proximo horario de uma rotina agendada. Ancora no horario PLANEJADO (nao em "now"),
    // evitando drift, e avanca em multiplos do intervalo ate ultrapassar "now" -> dispara UMA execucao
    // ao voltar de um downtime, realinhando a grade, sem repor as execucoes perdidas.
    public static class RoutineSchedule
    {
        public static DateTimeOffset ComputeNextExecution(DateTimeOffset? anchor, int intervalInMinutes, DateTimeOffset now)
        {
            int interval = Math.Max(1, intervalInMinutes);
            DateTimeOffset next = (anchor ?? now).AddMinutes(interval);

            while (next <= now)
            {
                next = next.AddMinutes(interval);
            }

            return next;
        }
    }
}

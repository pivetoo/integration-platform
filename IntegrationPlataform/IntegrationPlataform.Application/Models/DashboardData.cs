namespace IntegrationPlataform.Application.Models
{
    public class DashboardKpis
    {
        public long ActiveIntegrations { get; set; }

        public long ActiveConnectors { get; set; }

        public long ActivePipelines { get; set; }

        public long ExecutionsToday { get; set; }

        public double SuccessRate { get; set; }

        public long ErrorsToday { get; set; }
    }

    public class MonthlyExecutionSummary
    {
        public string Month { get; set; } = string.Empty;

        public long Success { get; set; }

        public long Error { get; set; }
    }

    public class RecentExecutionItem
    {
        public long Id { get; set; }

        public string Pipeline { get; set; } = string.Empty;

        public string Connector { get; set; } = string.Empty;

        public int Status { get; set; }

        public long? Duration { get; set; }

        public DateTimeOffset StartedAt { get; set; }
    }

    public class DashboardData
    {
        public DashboardKpis Kpis { get; set; } = new();

        public List<MonthlyExecutionSummary> MonthlyExecutions { get; set; } = [];

        public List<RecentExecutionItem> RecentExecutions { get; set; } = [];
    }
}

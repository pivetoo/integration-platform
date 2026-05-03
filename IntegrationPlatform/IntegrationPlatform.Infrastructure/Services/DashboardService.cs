using IntegrationPlatform.Application.Models;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace IntegrationPlatform.Infrastructure.Services
{
    public sealed class DashboardService : IDashboardService
    {
        private readonly DbContext dbContext;

        public DashboardService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<DashboardData> GetDashboardData(CancellationToken cancellationToken = default)
        {
            return new DashboardData
            {
                Kpis = await GetKpis(cancellationToken),
                MonthlyExecutions = await GetMonthlyExecutions(cancellationToken),
                RecentExecutions = await GetRecentExecutions(cancellationToken)
            };
        }

        private async Task<DashboardKpis> GetKpis(CancellationToken cancellationToken)
        {
            DateTimeOffset today = new(DateTime.UtcNow.Date, TimeSpan.Zero);

            long activeIntegrations = await (
                from integration in dbContext.Set<Integration>().AsNoTracking()
                where integration.IsActive
                select integration.Id)
                .LongCountAsync(cancellationToken);

            long activeConnectors = await (
                from connector in dbContext.Set<Connector>().AsNoTracking()
                where connector.IsActive
                select connector.Id)
                .LongCountAsync(cancellationToken);

            long activePipelines = await (
                from pipeline in dbContext.Set<Pipeline>().AsNoTracking()
                where pipeline.IsActive
                select pipeline.Id)
                .LongCountAsync(cancellationToken);

            List<Execution> executionsToday = await (
                from execution in dbContext.Set<Execution>().AsNoTracking()
                where execution.StartedAt >= today
                select execution)
                .ToListAsync(cancellationToken);

            long totalToday = executionsToday.Count;
            long errorsToday = executionsToday.Count(item => item.Status == ExecutionStatus.Error);
            long successToday = executionsToday.Count(item => item.Status == ExecutionStatus.Success);

            double successRate = totalToday > 0
                ? Math.Round((double)successToday / totalToday * 100, 1)
                : 0;

            return new DashboardKpis
            {
                ActiveIntegrations = activeIntegrations,
                ActiveConnectors = activeConnectors,
                ActivePipelines = activePipelines,
                ExecutionsToday = totalToday,
                ErrorsToday = errorsToday,
                SuccessRate = successRate
            };
        }

        private async Task<List<MonthlyExecutionSummary>> GetMonthlyExecutions(CancellationToken cancellationToken)
        {
            DateTimeOffset startMonth = new DateTimeOffset(DateTime.Today.AddMonths(-5).Year, DateTime.Today.AddMonths(-5).Month, 1, 0, 0, 0, TimeSpan.Zero);

            List<Execution> executions = await (
                from execution in dbContext.Set<Execution>().AsNoTracking()
                where execution.StartedAt >= startMonth
                select execution)
                .ToListAsync(cancellationToken);

            List<MonthlyExecutionSummary> result = [];
            CultureInfo culture = new("pt-BR");

            for (int index = 0; index < 6; index++)
            {
                DateTimeOffset month = startMonth.AddMonths(index);

                List<Execution> monthlyExecutions = executions
                    .Where(item => item.StartedAt.Year == month.Year && item.StartedAt.Month == month.Month)
                    .ToList();

                result.Add(new MonthlyExecutionSummary
                {
                    Month = month.ToString("MMM", culture),
                    Success = monthlyExecutions.LongCount(item => item.Status == ExecutionStatus.Success),
                    Error = monthlyExecutions.LongCount(item => item.Status == ExecutionStatus.Error)
                });
            }

            return result;
        }

        private async Task<List<RecentExecutionItem>> GetRecentExecutions(CancellationToken cancellationToken)
        {
            List<RecentExecutionItem> executions = await (
                from execution in dbContext.Set<Execution>().AsNoTracking()
                join connector in dbContext.Set<Connector>().AsNoTracking() on execution.ConnectorId equals connector.Id
                join pipeline in dbContext.Set<Pipeline>().AsNoTracking() on execution.PipelineId equals (long?)pipeline.Id into pipelineJoin
                from pipeline in pipelineJoin.DefaultIfEmpty()
                orderby execution.StartedAt descending
                select new RecentExecutionItem
                {
                    Id = execution.Id,
                    Pipeline = pipeline != null ? pipeline.Name : "-",
                    Connector = connector.Name,
                    Status = (int)execution.Status,
                    Duration = execution.Duration,
                    StartedAt = execution.StartedAt
                })
                .Take(6)
                .ToListAsync(cancellationToken);

            return executions;
        }
    }
}

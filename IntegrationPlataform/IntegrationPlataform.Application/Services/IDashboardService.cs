using IntegrationPlataform.Application.Models;

namespace IntegrationPlataform.Application.Services
{
    public interface IDashboardService
    {
        Task<DashboardData> GetDashboardData(CancellationToken cancellationToken = default);
    }
}

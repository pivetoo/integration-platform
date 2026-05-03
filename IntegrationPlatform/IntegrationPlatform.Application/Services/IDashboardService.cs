using IntegrationPlatform.Application.Models;

namespace IntegrationPlatform.Application.Services
{
    public interface IDashboardService
    {
        Task<DashboardData> GetDashboardData(CancellationToken cancellationToken = default);
    }
}

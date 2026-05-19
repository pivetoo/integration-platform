using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IntegrationPlatform.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationPlatform.Api.Controllers
{
    [AccessArea("dashboard.area")]
    public sealed class DashboardController : ApiControllerBase
    {
        private readonly IDashboardService dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            this.dashboardService = dashboardService;
        }

        [RequireAccess("dashboard.getDashboardData.description")]
        [GetEndpoint]
        public async Task<IActionResult> GetDashboardData(CancellationToken cancellationToken)
        {
            var data = await dashboardService.GetDashboardData(cancellationToken);
            return Http200(data);
        }
    }
}

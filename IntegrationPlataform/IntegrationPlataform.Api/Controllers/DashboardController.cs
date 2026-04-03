using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IntegrationPlataform.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationPlataform.Api.Controllers
{
    [RequireAccess]
    public sealed class DashboardController : ApiControllerBase
    {
        private readonly IDashboardService dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            this.dashboardService = dashboardService;
        }

        [GetEndpoint("")]
        public async Task<IActionResult> GetDashboardData(CancellationToken cancellationToken)
        {
            var data = await dashboardService.GetDashboardData(cancellationToken);
            return Http200(data);
        }
    }
}

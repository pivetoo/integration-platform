using Archon.Api.Attributes;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    [RequireAccess]
    public sealed class ExecutionLogsController : ReadOnlyController<ExecutionLog>
    {
        public ExecutionLogsController(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

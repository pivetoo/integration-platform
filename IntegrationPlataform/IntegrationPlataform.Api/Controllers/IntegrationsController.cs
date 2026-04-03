using Archon.Api.Attributes;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    [RequireAccess]
    public sealed class IntegrationsController : ReadOnlyController<Integration>
    {
        public IntegrationsController(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

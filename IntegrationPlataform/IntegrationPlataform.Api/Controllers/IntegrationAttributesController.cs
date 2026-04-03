using Archon.Api.Attributes;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    [RequireAccess]
    public sealed class IntegrationAttributesController : ReadOnlyController<IntegrationAttribute>
    {
        public IntegrationAttributesController(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

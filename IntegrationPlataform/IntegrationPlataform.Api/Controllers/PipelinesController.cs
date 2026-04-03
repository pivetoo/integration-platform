using Archon.Api.Attributes;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    [RequireAccess]
    public sealed class PipelinesController : ReadOnlyController<Pipeline>
    {
        public PipelinesController(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

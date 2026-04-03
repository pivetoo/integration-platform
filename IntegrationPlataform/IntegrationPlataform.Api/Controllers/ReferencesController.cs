using Archon.Api.Attributes;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    [RequireAccess]
    public sealed class ReferencesController : ReadOnlyController<Reference>
    {
        public ReferencesController(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

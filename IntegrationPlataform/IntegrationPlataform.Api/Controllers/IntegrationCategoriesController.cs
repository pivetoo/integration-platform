using Archon.Api.Attributes;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    [RequireAccess]
    public sealed class IntegrationCategoriesController : ReadOnlyController<IntegrationCategory>
    {
        public IntegrationCategoriesController(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

using Archon.Api.Attributes;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    [RequireAccess]
    public sealed class DatabaseScriptsController : ReadOnlyController<DatabaseScript>
    {
        public DatabaseScriptsController(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

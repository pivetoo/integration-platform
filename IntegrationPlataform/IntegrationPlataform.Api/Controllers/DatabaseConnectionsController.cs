using Archon.Api.Attributes;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    [RequireAccess]
    public sealed class DatabaseConnectionsController : ReadOnlyController<DatabaseConnection>
    {
        public DatabaseConnectionsController(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

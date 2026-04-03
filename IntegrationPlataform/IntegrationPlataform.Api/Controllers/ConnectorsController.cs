using Archon.Api.Attributes;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    [RequireAccess]
    public sealed class ConnectorsController : ReadOnlyController<Connector>
    {
        public ConnectorsController(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

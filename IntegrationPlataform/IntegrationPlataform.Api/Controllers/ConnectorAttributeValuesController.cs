using Archon.Api.Attributes;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    [RequireAccess]
    public sealed class ConnectorAttributeValuesController : ReadOnlyController<ConnectorAttributeValue>
    {
        public ConnectorAttributeValuesController(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

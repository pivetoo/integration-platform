using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class ConnectorService : CrudService<Connector>, IConnectorService
    {
        public ConnectorService(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

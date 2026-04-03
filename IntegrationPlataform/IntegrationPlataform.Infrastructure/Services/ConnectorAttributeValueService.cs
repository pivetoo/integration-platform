using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class ConnectorAttributeValueService : CrudService<ConnectorAttributeValue>, IConnectorAttributeValueService
    {
        public ConnectorAttributeValueService(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class IntegrationAttributeService : CrudService<IntegrationAttribute>, IIntegrationAttributeService
    {
        public IntegrationAttributeService(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

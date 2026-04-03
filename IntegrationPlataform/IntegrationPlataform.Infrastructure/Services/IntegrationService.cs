using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class IntegrationService : CrudService<Integration>, IIntegrationService
    {
        public IntegrationService(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

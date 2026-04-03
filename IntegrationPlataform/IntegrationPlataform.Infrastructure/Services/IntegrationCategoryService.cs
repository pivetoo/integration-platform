using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class IntegrationCategoryService : CrudService<IntegrationCategory>, IIntegrationCategoryService
    {
        public IntegrationCategoryService(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

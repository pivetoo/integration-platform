using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class DatabaseScriptService : CrudService<DatabaseScript>, IDatabaseScriptService
    {
        public DatabaseScriptService(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

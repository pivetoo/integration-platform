using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class DatabaseConnectionService : CrudService<DatabaseConnection>, IDatabaseConnectionService
    {
        public DatabaseConnectionService(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class ExecutionLogService : CrudService<ExecutionLog>, IExecutionLogService
    {
        public ExecutionLogService(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

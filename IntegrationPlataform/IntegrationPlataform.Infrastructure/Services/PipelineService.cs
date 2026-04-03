using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class PipelineService : CrudService<Pipeline>, IPipelineService
    {
        public PipelineService(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

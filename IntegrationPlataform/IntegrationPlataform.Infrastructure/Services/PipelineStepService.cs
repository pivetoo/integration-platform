using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class PipelineStepService : CrudService<PipelineStep>, IPipelineStepService
    {
        public PipelineStepService(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

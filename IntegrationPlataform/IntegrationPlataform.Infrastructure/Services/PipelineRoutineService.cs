using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class PipelineRoutineService : CrudService<PipelineRoutine>, IPipelineRoutineService
    {
        public PipelineRoutineService(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

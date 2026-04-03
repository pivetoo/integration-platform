using Archon.Api.Attributes;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    [RequireAccess]
    public sealed class PipelineRoutinesController : ReadOnlyController<PipelineRoutine>
    {
        public PipelineRoutinesController(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

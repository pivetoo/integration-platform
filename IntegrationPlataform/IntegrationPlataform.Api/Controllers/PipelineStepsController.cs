using Archon.Api.Attributes;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    [RequireAccess]
    public sealed class PipelineStepsController : ReadOnlyController<PipelineStep>
    {
        public PipelineStepsController(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

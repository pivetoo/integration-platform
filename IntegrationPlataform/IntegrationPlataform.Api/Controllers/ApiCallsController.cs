using Archon.Api.Attributes;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    [RequireAccess]
    public sealed class ApiCallsController : ReadOnlyController<ApiCall>
    {
        public ApiCallsController(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

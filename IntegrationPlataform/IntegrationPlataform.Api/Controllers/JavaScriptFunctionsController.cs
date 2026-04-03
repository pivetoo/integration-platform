using Archon.Api.Attributes;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    [RequireAccess]
    public sealed class JavaScriptFunctionsController : ReadOnlyController<JavaScriptFunction>
    {
        public JavaScriptFunctionsController(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

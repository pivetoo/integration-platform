using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class JavaScriptFunctionService : CrudService<JavaScriptFunction>, IJavaScriptFunctionService
    {
        public JavaScriptFunctionService(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

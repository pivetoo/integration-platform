using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class ApiCallService : CrudService<ApiCall>, IApiCallService
    {
        public ApiCallService(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

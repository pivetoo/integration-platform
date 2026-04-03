using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class ReferenceService : CrudService<Reference>, IReferenceService
    {
        public ReferenceService(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

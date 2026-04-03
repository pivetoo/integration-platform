using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class ProcessingQueueService : CrudService<ProcessingQueue>, IProcessingQueueService
    {
        public ProcessingQueueService(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

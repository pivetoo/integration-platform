using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public abstract class ReadOnlyController<T> : ApiControllerBase where T : Entity
    {
        private readonly DbContext dbContext;

        protected ReadOnlyController(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [GetEndpoint("")]
        public virtual async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            var result = await dbContext.Set<T>()
                .AsNoTracking()
                .OrderByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);

            return Http200(result);
        }

        [GetEndpoint("{id:long}")]
        public virtual async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return Http400("Id is required.");
            }

            T? entity = await dbContext.Set<T>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            return entity is null ? Http404("Record not found.") : Http200(entity);
        }
    }
}

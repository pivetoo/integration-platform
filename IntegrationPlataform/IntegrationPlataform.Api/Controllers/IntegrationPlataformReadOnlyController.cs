using Archon.Api.Controllers;
using Archon.Core.Entities;
using IntegrationPlataform.Application.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Api.Controllers
{
    public abstract class IntegrationPlataformReadOnlyController<T> : ReadOnlyController<T> where T : Entity
    {
        protected IntegrationPlataformReadOnlyController(DbContext dbContext) : base(dbContext)
        {
        }

        protected new IStringLocalizer<IntegrationPlataformResource> Localizer => HttpContext.RequestServices.GetRequiredService<IStringLocalizer<IntegrationPlataformResource>>();
    }
}

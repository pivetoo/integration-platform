using Archon.Api.Controllers;
using IntegrationPlataform.Application.Localization;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Api.Controllers
{
    public abstract class IntegrationPlataformControllerBase : ApiControllerBase
    {
        protected new IStringLocalizer<IntegrationPlataformResource> Localizer => HttpContext.RequestServices.GetRequiredService<IStringLocalizer<IntegrationPlataformResource>>();
    }
}

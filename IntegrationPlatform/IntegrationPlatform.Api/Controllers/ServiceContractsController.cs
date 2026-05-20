using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.ServiceContracts;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Api.Controllers
{
    [AccessArea("serviceContracts.area")]
    public sealed class ServiceContractsController : ApiControllerBase
    {
        private readonly IServiceContractService serviceContractService;
        private new IStringLocalizer<IntegrationPlatformResource> Localizer { get; }

        public ServiceContractsController(IServiceContractService serviceContractService, IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            this.serviceContractService = serviceContractService;
            Localizer = localizer;
        }

        [RequireAccess("serviceContracts.get.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] string? search, [FromQuery] long? integrationCategoryId, CancellationToken cancellationToken)
        {
            PagedResult<ServiceContract> result = await serviceContractService.GetServiceContracts(request, search, integrationCategoryId, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("serviceContracts.getById.description")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return Http400(Localizer["request.id.required"]);
            }

            ServiceContract? entity = await serviceContractService.GetServiceContractById(id, cancellationToken);
            return entity is null ? Http404(Localizer["record.notFound"]) : Http200(entity);
        }

        [RequireAccess("serviceContracts.getByIdentifier.description")]
        [GetEndpoint("by-identifier/{identifier}")]
        public async Task<IActionResult> GetByIdentifier(string identifier, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return Http400(Localizer["request.id.required"]);
            }

            ServiceContract? entity = await serviceContractService.GetServiceContractByIdentifier(identifier, cancellationToken);
            return entity is null ? Http404(Localizer["record.notFound"]) : Http200(entity);
        }

        [RequireAccess("serviceContracts.getActive.description")]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            List<ServiceContract> result = await serviceContractService.GetActiveServiceContracts(cancellationToken);
            return Http200(result);
        }

        [RequireAccess("serviceContracts.getByIntegration.description")]
        [GetEndpoint("by-integration/{integrationId:long}")]
        public async Task<IActionResult> GetByIntegration(long integrationId, CancellationToken cancellationToken)
        {
            List<ServiceContract> result = await serviceContractService.GetServiceContractsByIntegration(integrationId, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("serviceContracts.create.description")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateServiceContractRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            ServiceContract entity = await serviceContractService.CreateServiceContract(request, cancellationToken);
            return Http201(entity, Localizer["serviceContract.created"]);
        }

        [RequireAccess("serviceContracts.update.description")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateServiceContractRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            ServiceContract entity = await serviceContractService.UpdateServiceContract(id, request, cancellationToken);
            return Http200(entity, Localizer["serviceContract.updated"]);
        }

        [RequireAccess("serviceContracts.delete.description")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            ServiceContract? entity = await serviceContractService.Delete(id, cancellationToken);
            if (entity is null)
            {
                return Http404(serviceContractService.GetErrorMessages());
            }

            return Http200(entity, Localizer["serviceContract.deleted"]);
        }

        [RequireAccess("serviceContracts.setIntegration.description")]
        [PostEndpoint("integration-binding")]
        public async Task<IActionResult> SetIntegrationBinding([FromBody] SetIntegrationServiceContractRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            IntegrationServiceContract entity = await serviceContractService.SetIntegrationServiceContract(request, cancellationToken);
            return Http200(entity, Localizer["serviceContract.integration.bound"]);
        }

        [RequireAccess("serviceContracts.removeIntegration.description")]
        [DeleteEndpoint("integration-binding/{integrationId:long}/{serviceContractId:long}")]
        public async Task<IActionResult> RemoveIntegrationBinding(long integrationId, long serviceContractId, CancellationToken cancellationToken)
        {
            bool removed = await serviceContractService.RemoveIntegrationServiceContract(integrationId, serviceContractId, cancellationToken);
            if (!removed)
            {
                return Http404(Localizer["record.notFound"]);
            }

            return Http200(true, Localizer["serviceContract.integration.unbound"]);
        }
    }
}

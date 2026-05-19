using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlatform.Api.Contracts.References;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.References;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Api.Controllers
{
    public sealed class ReferencesController : ApiControllerBase
    {
        private readonly IReferenceService referenceService;
        private new IStringLocalizer<IntegrationPlatformResource> Localizer { get; }
        private static readonly Func<Reference, ReferenceContract> MapReference = ReferenceContract.Projection.Compile();

        public ReferencesController(IReferenceService referenceService, IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            this.referenceService = referenceService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar as referências internas e externas registradas na plataforma.")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] string? search, CancellationToken cancellationToken)
        {
            PagedResult<Reference> result = await referenceService.GetReferences(request, search, cancellationToken);
            return Http200(new PagedResult<ReferenceContract>
            {
                Items = result.Items.Select(MapReference).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de uma referência específica.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            Reference? reference = await referenceService.GetReferenceById(id, cancellationToken);

            return reference is null ? Http404(Localizer["reference.notFound"]) : Http200(MapReference(reference));
        }

        [RequireAccess("Permite listar as referências vinculadas a um conector específico.")]
        [GetEndpoint("connector/{connectorId:long}")]
        public async Task<IActionResult> GetByConnector(long connectorId, CancellationToken cancellationToken)
        {
            if (connectorId <= 0)
            {
                return Http400(Localizer["request.connector.id.required"]);
            }

            List<Reference> references = await referenceService.GetReferencesByConnector(connectorId, cancellationToken);

            return Http200(references.Select(MapReference).ToList());
        }

        [RequireAccess("Permite cadastrar uma nova referência entre identificadores internos e externos.")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateReferenceRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Reference reference = await referenceService.CreateReference(request, cancellationToken);
            return Http201(MapReference(reference), Localizer["reference.created"]);
        }

        [RequireAccess("Permite atualizar uma referência entre identificadores internos e externos.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateReferenceRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Reference updatedReference = await referenceService.UpdateReference(id, request, cancellationToken);
            return Http200(MapReference(updatedReference), Localizer["reference.updated"]);
        }

        [RequireAccess("Permite excluir uma referência cadastrada na plataforma.")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            Reference? reference = await referenceService.Delete(id, cancellationToken);
            if (reference is null)
            {
                return Http404(referenceService.GetErrorMessages());
            }

            return Http200(MapReference(reference), Localizer["reference.deleted"]);
        }
    }
}

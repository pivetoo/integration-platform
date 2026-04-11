using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Api.Contracts.PipelineStepValueMappings;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.PipelineStepValueMappings;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class PipelineStepValueMappingsController : ApiControllerBase
    {
        private readonly IPipelineStepValueMappingService pipelineStepValueMappingService;
        private static readonly Func<PipelineStepValueMapping, PipelineStepValueMappingContract> MapPipelineStepValueMapping = PipelineStepValueMappingContract.Projection.Compile();
        private new IStringLocalizer<IntegrationPlataformResource> Localizer { get; }

        public PipelineStepValueMappingsController(IPipelineStepValueMappingService pipelineStepValueMappingService, IStringLocalizer<IntegrationPlataformResource> localizer)
        {
            this.pipelineStepValueMappingService = pipelineStepValueMappingService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os mapeamentos visuais de valores cadastrados para etapas do pipeline.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<PipelineStepValueMapping> result = await pipelineStepValueMappingService.GetPipelineStepValueMappings(request, cancellationToken);
            return Http200(new PagedResult<PipelineStepValueMappingContract>
            {
                Items = result.Items.Select(MapPipelineStepValueMapping).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de um mapeamento visual de valor específico.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            PipelineStepValueMapping? entity = await pipelineStepValueMappingService.GetPipelineStepValueMappingById(id, cancellationToken);
            return entity is null ? Http404(Localizer["pipelineStepValueMapping.notFound"]) : Http200(MapPipelineStepValueMapping(entity));
        }

        [RequireAccess("Permite listar os mapeamentos visuais vinculados a uma etapa específica do pipeline.")]
        [GetEndpoint("pipeline-step/{pipelineStepId:long}")]
        public async Task<IActionResult> GetByPipelineStep(long pipelineStepId, CancellationToken cancellationToken)
        {
            if (pipelineStepId <= 0)
            {
                return Http400(Localizer["request.pipelineStep.id.required"]);
            }

            List<PipelineStepValueMapping> entities = await pipelineStepValueMappingService.GetByPipelineStep(pipelineStepId, cancellationToken);
            return Http200(entities.Select(MapPipelineStepValueMapping).ToList());
        }

        [RequireAccess("Permite cadastrar um novo mapeamento visual de valor para uma etapa do pipeline.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreatePipelineStepValueMappingRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            PipelineStepValueMapping entity = await pipelineStepValueMappingService.CreatePipelineStepValueMapping(request, cancellationToken);
            return Http201(MapPipelineStepValueMapping(entity), Localizer["pipelineStepValueMapping.created"]);
        }

        [RequireAccess("Permite atualizar um mapeamento visual de valor cadastrado.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdatePipelineStepValueMappingRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            PipelineStepValueMapping entity = await pipelineStepValueMappingService.UpdatePipelineStepValueMapping(id, request, cancellationToken);
            return Http200(MapPipelineStepValueMapping(entity), Localizer["pipelineStepValueMapping.updated"]);
        }

        [RequireAccess("Permite excluir um mapeamento visual de valor cadastrado na plataforma.")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            PipelineStepValueMapping? entity = await pipelineStepValueMappingService.Delete(id, cancellationToken);
            if (entity is null)
            {
                return Http404(pipelineStepValueMappingService.GetErrorMessages());
            }

            return Http200(MapPipelineStepValueMapping(entity), Localizer["pipelineStepValueMapping.deleted"]);
        }
    }
}

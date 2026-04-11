using Archon.Core.Templating;
using IntegrationPlataform.Application.Models;
using IntegrationPlataform.Application.Requests.Designer;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using IntegrationPlataform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class DesignerSupportService : IDesignerSupportService
    {
        private readonly DbContext dbContext;

        public DesignerSupportService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<List<DesignerFieldSuggestion>> GetFieldSuggestions(long? connectorId = null, long? pipelineExampleId = null, long? pipelineStepExampleId = null, string? payloadExample = null, CancellationToken cancellationToken = default)
        {
            List<DesignerFieldSuggestion> suggestions = [];

            if (connectorId.HasValue)
            {
                Connector? connector = await dbContext.Set<Connector>()
                    .AsNoTracking()
                    .Include(item => item.AttributeValues)
                        .ThenInclude(item => item.IntegrationAttribute)
                    .FirstOrDefaultAsync(item => item.Id == connectorId.Value, cancellationToken);

                if (connector is not null)
                {
                    suggestions.AddRange(connector.AttributeValues
                        .OrderBy(item => item.IntegrationAttribute.Order)
                        .Select(item => new DesignerFieldSuggestion
                        {
                            Key = item.IntegrationAttribute.Field,
                            Label = item.IntegrationAttribute.Label,
                            Source = "connector",
                            SourceName = connector.Name,
                            Type = item.IntegrationAttribute.Type.ToString(),
                            ExampleValue = item.Value
                        }));
                }
            }

            string? effectivePayloadExample = payloadExample;
            if (string.IsNullOrWhiteSpace(effectivePayloadExample) && pipelineExampleId.HasValue)
            {
                effectivePayloadExample = await dbContext.Set<PipelineExample>()
                    .AsNoTracking()
                    .Where(item => item.Id == pipelineExampleId.Value)
                    .Select(item => item.InputPayloadExample)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(effectivePayloadExample))
            {
                suggestions.AddRange(ExtractSuggestionsFromJson(effectivePayloadExample, "payload", null, pipelineExampleId));
            }

            if (pipelineStepExampleId.HasValue)
            {
                PipelineStepExample? stepExample = await dbContext.Set<PipelineStepExample>()
                    .AsNoTracking()
                    .Include(item => item.PipelineStep)
                    .FirstOrDefaultAsync(item => item.Id == pipelineStepExampleId.Value, cancellationToken);

                if (stepExample is not null && !string.IsNullOrWhiteSpace(stepExample.ResponseExample))
                {
                    suggestions.AddRange(ExtractSuggestionsFromJson(stepExample.ResponseExample, "step", stepExample.PipelineStep?.Name ?? stepExample.Name, stepExample.PipelineExampleId, stepExample.PipelineStepId));
                }
            }

            return suggestions
                .GroupBy(item => new { item.Source, item.Key, item.PipelineStepId, item.PipelineExampleId })
                .Select(group => group.First())
                .OrderBy(item => item.Source)
                .ThenBy(item => item.Key)
                .ToList();
        }

        public async Task<ApiCallPreviewResult> GenerateApiCallPreview(GenerateApiCallPreviewRequest request, CancellationToken cancellationToken = default)
        {
            ApiCall apiCall = await dbContext.Set<ApiCall>()
                .AsNoTracking()
                .FirstAsync(item => item.Id == request.ApiCallId, cancellationToken);

            Dictionary<string, string> connectorAttributes = await GetConnectorAttributes(request.ConnectorId, cancellationToken);
            string? effectivePayload = await ResolvePayloadExample(request.PayloadExample, request.PipelineExampleId, cancellationToken);
            Dictionary<string, object> payloadData = ParsePayloadDictionary(effectivePayload);
            Dictionary<string, object> stepVariables = await ResolveStepVariables(request.PipelineStepExampleId, cancellationToken);

            return BuildApiCallPreview(apiCall, payloadData, stepVariables, connectorAttributes);
        }

        public async Task<PipelineStepPreviewResult> GeneratePipelineStepPreview(GeneratePipelineStepPreviewRequest request, CancellationToken cancellationToken = default)
        {
            PipelineStep step = await dbContext.Set<PipelineStep>()
                .AsNoTracking()
                .Include(item => item.ApiCall)
                .Include(item => item.ValueMappings)
                .Include(item => item.Examples)
                .FirstAsync(item => item.Id == request.PipelineStepId, cancellationToken);

            Dictionary<string, string> connectorAttributes = await GetConnectorAttributes(request.ConnectorId, cancellationToken);
            string? effectivePayload = await ResolvePayloadExample(request.PayloadExample, request.PipelineExampleId, cancellationToken);
            Dictionary<string, object> payloadData = ParsePayloadDictionary(effectivePayload);
            Dictionary<string, object> stepVariables = BuildStepVariablesFromMappings(step.ValueMappings, payloadData, connectorAttributes);
            PipelineStepExample? stepExample = step.Examples.OrderByDescending(item => item.IsDefault).ThenBy(item => item.Id).FirstOrDefault();

            if (step.Type == PipelineStepType.HttpRequest && step.ApiCall is not null)
            {
                ApiCallPreviewResult preview = BuildApiCallPreview(step.ApiCall, payloadData, stepVariables, connectorAttributes);
                return new PipelineStepPreviewResult
                {
                    PipelineStepId = step.Id,
                    StepName = step.Name,
                    StepType = step.Type.ToString(),
                    Url = preview.Url,
                    Headers = preview.Headers,
                    Body = preview.Body,
                    RequestExample = stepExample?.RequestExample,
                    ResponseExample = stepExample?.ResponseExample
                };
            }

            return new PipelineStepPreviewResult
            {
                PipelineStepId = step.Id,
                StepName = step.Name,
                StepType = step.Type.ToString(),
                RequestExample = stepExample?.RequestExample,
                ResponseExample = stepExample?.ResponseExample
            };
        }

        private async Task<Dictionary<string, string>> GetConnectorAttributes(long? connectorId, CancellationToken cancellationToken)
        {
            if (!connectorId.HasValue)
            {
                return [];
            }

            Connector? connector = await dbContext.Set<Connector>()
                .AsNoTracking()
                .Include(item => item.AttributeValues)
                    .ThenInclude(item => item.IntegrationAttribute)
                .FirstOrDefaultAsync(item => item.Id == connectorId.Value, cancellationToken);

            return connector is null
                ? []
                : connector.AttributeValues.ToDictionary(
                    item => item.IntegrationAttribute.Field,
                    item => item.Value,
                    StringComparer.OrdinalIgnoreCase);
        }

        private async Task<string?> ResolvePayloadExample(string? payloadExample, long? pipelineExampleId, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(payloadExample) || !pipelineExampleId.HasValue)
            {
                return payloadExample;
            }

            return await dbContext.Set<PipelineExample>()
                .AsNoTracking()
                .Where(item => item.Id == pipelineExampleId.Value)
                .Select(item => item.InputPayloadExample)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<Dictionary<string, object>> ResolveStepVariables(long? pipelineStepExampleId, CancellationToken cancellationToken)
        {
            if (!pipelineStepExampleId.HasValue)
            {
                return [];
            }

            PipelineStepExample? stepExample = await dbContext.Set<PipelineStepExample>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == pipelineStepExampleId.Value, cancellationToken);

            return stepExample is not null && !string.IsNullOrWhiteSpace(stepExample.ResponseExample)
                ? ParsePayloadDictionary(stepExample.ResponseExample)
                : [];
        }

        private static ApiCallPreviewResult BuildApiCallPreview(ApiCall apiCall, Dictionary<string, object> payloadData, Dictionary<string, object> stepVariables, Dictionary<string, string> connectorAttributes)
        {
            string url = TemplateInterpolator.Interpolate(apiCall.Url, stepVariables, payloadData, connectorAttributes);
            string headersTemplate = TemplateInterpolator.Interpolate(apiCall.HeadersTemplate, stepVariables, payloadData, connectorAttributes);
            string? body = string.IsNullOrWhiteSpace(apiCall.BodyTemplate)
                ? null
                : TemplateInterpolator.Interpolate(apiCall.BodyTemplate, stepVariables, payloadData, connectorAttributes);

            Dictionary<string, string> headers = [];
            if (!string.IsNullOrWhiteSpace(headersTemplate))
            {
                try
                {
                    headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersTemplate) ?? [];
                }
                catch
                {
                    headers = [];
                }
            }

            return new ApiCallPreviewResult
            {
                Method = apiCall.Method.ToString().ToUpperInvariant(),
                Url = url,
                Headers = headers,
                Body = body
            };
        }

        private static Dictionary<string, object> BuildStepVariablesFromMappings(IEnumerable<PipelineStepValueMapping> mappings, Dictionary<string, object> payloadData, Dictionary<string, string> connectorAttributes)
        {
            Dictionary<string, object> variables = [];

            foreach (PipelineStepValueMapping mapping in mappings.OrderBy(item => item.Order))
            {
                object? resolved = ResolveMappingValue(mapping, payloadData, connectorAttributes);
                if (resolved is not null)
                {
                    variables[mapping.TargetField] = resolved;
                }
            }

            return variables;
        }

        private static object? ResolveMappingValue(PipelineStepValueMapping mapping, Dictionary<string, object> payloadData, Dictionary<string, string> connectorAttributes)
        {
            if (string.Equals(mapping.SourceType, "fixed", StringComparison.OrdinalIgnoreCase))
            {
                return mapping.FixedValue ?? mapping.SourcePath;
            }

            if (string.Equals(mapping.SourceType, "connector", StringComparison.OrdinalIgnoreCase))
            {
                return connectorAttributes.TryGetValue(mapping.SourcePath, out string? connectorValue)
                    ? connectorValue
                    : null;
            }

            if (string.Equals(mapping.SourceType, "payload", StringComparison.OrdinalIgnoreCase))
            {
                return ResolvePathValue(payloadData, mapping.SourcePath);
            }

            return null;
        }

        private static object? ResolvePathValue(Dictionary<string, object> payloadData, string path)
        {
            string normalizedPath = Regex.Replace(path, @"\[(\d+)\]", ".$1");
            string[] parts = normalizedPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
            object? current = payloadData;

            foreach (string part in parts)
            {
                if (current is Dictionary<string, object> dictionary)
                {
                    if (!dictionary.TryGetValue(part, out current))
                    {
                        return null;
                    }
                }
                else if (current is JsonElement jsonElement)
                {
                    if (jsonElement.ValueKind == JsonValueKind.Object && jsonElement.TryGetProperty(part, out JsonElement property))
                    {
                        current = property;
                    }
                    else if (jsonElement.ValueKind == JsonValueKind.Array && int.TryParse(part, out int index) && jsonElement.GetArrayLength() > index)
                    {
                        current = jsonElement[index];
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (current is JsonObject jsonObject)
                {
                    current = jsonObject[part];
                }
                else if (current is JsonArray jsonArray && int.TryParse(part, out int index) && jsonArray.Count > index)
                {
                    current = jsonArray[index];
                }
                else
                {
                    return null;
                }
            }

            return current is JsonNode node ? node.ToJsonString() : current;
        }

        private static Dictionary<string, object> ParsePayloadDictionary(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? [];
            }
            catch
            {
                return [];
            }
        }

        private static List<DesignerFieldSuggestion> ExtractSuggestionsFromJson(string json, string source, string? sourceName, long? pipelineExampleId, long? pipelineStepId = null)
        {
            List<DesignerFieldSuggestion> suggestions = [];

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                WalkElement(document.RootElement, string.Empty, source, sourceName, pipelineExampleId, pipelineStepId, suggestions);
            }
            catch
            {
            }

            return suggestions;
        }

        private static void WalkElement(JsonElement element, string path, string source, string? sourceName, long? pipelineExampleId, long? pipelineStepId, List<DesignerFieldSuggestion> suggestions)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (JsonProperty property in element.EnumerateObject())
                    {
                        string childPath = string.IsNullOrWhiteSpace(path) ? property.Name : $"{path}.{property.Name}";
                        WalkElement(property.Value, childPath, source, sourceName, pipelineExampleId, pipelineStepId, suggestions);
                    }
                    break;

                case JsonValueKind.Array:
                    int index = 0;
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        string childPath = $"{path}[{index}]";
                        WalkElement(item, childPath, source, sourceName, pipelineExampleId, pipelineStepId, suggestions);
                        index++;
                    }
                    if (index == 0 && !string.IsNullOrWhiteSpace(path))
                    {
                        suggestions.Add(new DesignerFieldSuggestion
                        {
                            Key = path,
                            Label = path,
                            Source = source,
                            SourceName = sourceName,
                            Type = "array",
                            ExampleValue = "[]",
                            PipelineExampleId = pipelineExampleId,
                            PipelineStepId = pipelineStepId
                        });
                    }
                    break;

                default:
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        break;
                    }

                    suggestions.Add(new DesignerFieldSuggestion
                    {
                        Key = path,
                        Label = path,
                        Source = source,
                        SourceName = sourceName,
                        Type = ResolveType(element),
                        ExampleValue = ResolveExampleValue(element),
                        PipelineExampleId = pipelineExampleId,
                        PipelineStepId = pipelineStepId
                    });
                    break;
            }
        }

        private static string ResolveType(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => "string",
                JsonValueKind.Number => "number",
                JsonValueKind.True or JsonValueKind.False => "boolean",
                JsonValueKind.Array => "array",
                JsonValueKind.Object => "object",
                JsonValueKind.Null => "null",
                _ => "unknown"
            };
        }

        private static string? ResolveExampleValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => null,
                _ => element.GetRawText()
            };
        }
    }
}

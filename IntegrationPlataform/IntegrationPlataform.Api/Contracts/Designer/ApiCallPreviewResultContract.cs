using IntegrationPlataform.Application.Models;

namespace IntegrationPlataform.Api.Contracts.Designer
{
    public sealed class ApiCallPreviewResultContract
    {
        public string Method { get; init; } = string.Empty;

        public string Url { get; init; } = string.Empty;

        public Dictionary<string, string> Headers { get; init; } = [];

        public string? Body { get; init; }

        public static ApiCallPreviewResultContract FromModel(ApiCallPreviewResult model)
        {
            return new ApiCallPreviewResultContract
            {
                Method = model.Method,
                Url = model.Url,
                Headers = model.Headers,
                Body = model.Body
            };
        }
    }
}

namespace IntegrationPlataform.Application.Models
{
    public sealed class ApiCallPreviewResult
    {
        public string Method { get; init; } = string.Empty;

        public string Url { get; init; } = string.Empty;

        public Dictionary<string, string> Headers { get; init; } = [];

        public string? Body { get; init; }
    }
}

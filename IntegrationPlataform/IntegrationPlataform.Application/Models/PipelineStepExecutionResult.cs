namespace IntegrationPlataform.Application.Models
{
    public class PipelineStepExecutionResult
    {
        public bool Success { get; set; }

        public string? RequestInfo { get; set; }

        public string? ResponseBody { get; set; }

        public int? StatusCode { get; set; }

        public long DurationInMilliseconds { get; set; }

        public string? Error { get; set; }

        public object? ExtractedResult { get; set; }
    }
}

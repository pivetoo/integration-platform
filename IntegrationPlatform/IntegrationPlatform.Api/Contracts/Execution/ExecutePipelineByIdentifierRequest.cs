namespace IntegrationPlatform.Api.Contracts.Execution
{
    public sealed class ExecutePipelineByIdentifierRequest
    {
        public Dictionary<string, object> InputData { get; set; } = [];
    }
}

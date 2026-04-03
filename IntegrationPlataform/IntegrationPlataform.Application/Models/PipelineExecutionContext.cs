using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Models
{
    public class PipelineExecutionContext
    {
        public Connector Connector { get; set; } = null!;

        public Pipeline Pipeline { get; set; } = null!;

        public Execution Execution { get; set; } = null!;

        public Dictionary<string, string> ConnectorAttributes { get; set; } = [];

        public Dictionary<string, object> StepVariables { get; set; } = [];

        public Dictionary<string, object> PayloadData { get; set; } = [];

        public bool HasError { get; set; }

        public string? LastError { get; set; }
    }
}

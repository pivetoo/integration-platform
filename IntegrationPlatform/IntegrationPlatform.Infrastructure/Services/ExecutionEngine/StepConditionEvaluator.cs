using IntegrationPlatform.Application.Models;
using Jint;

namespace IntegrationPlatform.Infrastructure.Services.ExecutionEngine
{
    // Avalia a runcondition de um step: expressao JavaScript booleana com o mesmo escopo dos steps
    // JavaScript (variables/payload/attributes, sem atributos sensiveis). Falsy = pular o step.
    public static class StepConditionEvaluator
    {
        public static StepConditionResult Evaluate(string? condition, PipelineExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                return StepConditionResult.Run();
            }

            try
            {
                Engine engine = new(cfg => cfg
                    .TimeoutInterval(TimeSpan.FromSeconds(5))
                    .LimitMemory(10_000_000)
                    .LimitRecursion(50));

                Dictionary<string, string> scriptAttributes = context.ConnectorAttributes
                    .Where(pair => !context.SensitiveAttributeFields.Contains(pair.Key))
                    .ToDictionary(pair => pair.Key, pair => pair.Value);

                engine.SetValue("variables", context.StepVariables);
                engine.SetValue("payload", context.PayloadData);
                engine.SetValue("attributes", scriptAttributes);

                bool shouldRun = engine.Evaluate($"!!({condition})").AsBoolean();
                return shouldRun ? StepConditionResult.Run() : StepConditionResult.Skip();
            }
            catch (Exception exception)
            {
                return StepConditionResult.Fail(exception.Message);
            }
        }
    }

    public sealed class StepConditionResult
    {
        public bool ShouldRun { get; private init; }

        public string? Error { get; private init; }

        public static StepConditionResult Run() => new() { ShouldRun = true };

        public static StepConditionResult Skip() => new() { ShouldRun = false };

        public static StepConditionResult Fail(string error) => new() { ShouldRun = false, Error = error };
    }
}

using IntegrationPlatform.Application.Models;
using IntegrationPlatform.Infrastructure.Services.ExecutionEngine;

namespace IntegrationPlatform.Testing.Infrastructure.Services.ExecutionEngine
{
    [TestFixture]
    public class StepConditionEvaluatorTests
    {
        [Test]
        public void Evaluate_NullOrWhitespaceCondition_AlwaysRuns()
        {
            PipelineExecutionContext context = new();

            Assert.That(StepConditionEvaluator.Evaluate(null, context).ShouldRun, Is.True);
            Assert.That(StepConditionEvaluator.Evaluate("", context).ShouldRun, Is.True);
            Assert.That(StepConditionEvaluator.Evaluate("   ", context).ShouldRun, Is.True);
        }

        [Test]
        public void Evaluate_TruthyVariable_Runs()
        {
            PipelineExecutionContext context = new();
            context.StepVariables["existingCustomerId"] = "cus_000001";

            StepConditionResult result = StepConditionEvaluator.Evaluate("variables.existingCustomerId", context);

            Assert.That(result.ShouldRun, Is.True);
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public void Evaluate_FalsyVariable_Skips()
        {
            PipelineExecutionContext context = new();
            context.StepVariables["existingCustomerId"] = "";

            StepConditionResult result = StepConditionEvaluator.Evaluate("variables.existingCustomerId", context);

            Assert.That(result.ShouldRun, Is.False);
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public void Evaluate_NegatedFalsyVariable_Runs()
        {
            PipelineExecutionContext context = new();
            context.StepVariables["existingCustomerId"] = "";

            StepConditionResult result = StepConditionEvaluator.Evaluate("!variables.existingCustomerId", context);

            Assert.That(result.ShouldRun, Is.True);
        }

        [Test]
        public void Evaluate_MissingVariable_IsFalsy()
        {
            PipelineExecutionContext context = new();

            StepConditionResult result = StepConditionEvaluator.Evaluate("variables.doesNotExist", context);

            Assert.That(result.ShouldRun, Is.False);
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public void Evaluate_PayloadAndAttributesInScope()
        {
            PipelineExecutionContext context = new();
            context.PayloadData["method"] = "pix";
            context.ConnectorAttributes["base_url"] = "https://api.asaas.com/v3";

            StepConditionResult byPayload = StepConditionEvaluator.Evaluate("payload.method === 'pix'", context);
            StepConditionResult byAttribute = StepConditionEvaluator.Evaluate("attributes.base_url.indexOf('asaas') >= 0", context);

            Assert.That(byPayload.ShouldRun, Is.True);
            Assert.That(byAttribute.ShouldRun, Is.True);
        }

        [Test]
        public void Evaluate_SensitiveAttribute_NotExposed()
        {
            PipelineExecutionContext context = new();
            context.ConnectorAttributes["api_key"] = "$aact_prod_secret";
            context.SensitiveAttributeFields.Add("api_key");

            StepConditionResult result = StepConditionEvaluator.Evaluate("!attributes.api_key", context);

            Assert.That(result.ShouldRun, Is.True);
        }

        [Test]
        public void Evaluate_InvalidExpression_ReturnsError()
        {
            PipelineExecutionContext context = new();

            StepConditionResult result = StepConditionEvaluator.Evaluate("isto nao e js valido {", context);

            Assert.That(result.ShouldRun, Is.False);
            Assert.That(result.Error, Is.Not.Null);
        }

        [Test]
        public void Evaluate_NumericComparison_Works()
        {
            PipelineExecutionContext context = new();
            context.StepVariables["totalCount"] = 0L;

            StepConditionResult skip = StepConditionEvaluator.Evaluate("variables.totalCount > 0", context);
            StepConditionResult run = StepConditionEvaluator.Evaluate("variables.totalCount === 0", context);

            Assert.That(skip.ShouldRun, Is.False);
            Assert.That(run.ShouldRun, Is.True);
        }
    }
}

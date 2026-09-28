using IntegrationPlatform.Infrastructure.Services.Contracts;

namespace IntegrationPlatform.Testing.Infrastructure.Contracts
{
    [TestFixture]
    public sealed class InputSchemaValidatorTests
    {
        private static IReadOnlyList<string> Check(string schema, string? input)
        {
            return InputSchemaValidator.Validate(ContractSchemaParser.Parse(schema), input);
        }

        [Test]
        public void Missing_required_field_is_reported()
        {
            Check("{\"pixKey\":\"string\"}", "{}").Should().Equal("pixKey: required");
        }

        [Test]
        public void Null_value_counts_as_absent()
        {
            Check("{\"pixKey\":\"string\"}", "{\"pixKey\":null}").Should().Equal("pixKey: required");
            Check("{\"pixKey\":\"string?\"}", "{\"pixKey\":null}").Should().BeEmpty();
        }

        [TestCase("string", "{\"f\":1}", "f: expected string")]
        [TestCase("number", "{\"f\":\"10\"}", "f: expected number")]
        [TestCase("bool", "{\"f\":\"true\"}", "f: expected bool")]
        [TestCase("string[]", "{\"f\":\"a\"}", "f: expected array")]
        [TestCase("{a}", "{\"f\":[]}", "f: expected object")]
        public void Wrong_type_is_reported(string description, string input, string expected)
        {
            Check($"{{\"f\":\"{description}\"}}", input).Should().Equal(expected);
        }

        [TestCase("string", "{\"f\":\"x\"}")]
        [TestCase("number", "{\"f\":10}")]
        [TestCase("number", "{\"f\":10.5}")]
        [TestCase("bool", "{\"f\":false}")]
        [TestCase("string[]", "{\"f\":[]}")]
        [TestCase("{a}", "{\"f\":{}}")]
        public void Right_type_passes(string description, string input)
        {
            Check($"{{\"f\":\"{description}\"}}", input).Should().BeEmpty();
        }

        [Test]
        public void Enum_accepts_any_case_and_rejects_other_values()
        {
            const string schema = "{\"k\":\"Cpf|Cnpj\"}";

            Check(schema, "{\"k\":\"cpf\"}").Should().BeEmpty();
            Check(schema, "{\"k\":\"Telefone\"}").Should().Equal("k: expected one of Cpf|Cnpj");
            Check(schema, "{\"k\":5}").Should().Equal("k: expected one of Cpf|Cnpj");
        }

        [Test]
        public void Unknown_type_only_requires_presence()
        {
            const string schema = "{\"k\":\"boleto\"}";

            Check(schema, "{\"k\":123}").Should().BeEmpty();
            Check(schema, "{\"k\":\"x\"}").Should().BeEmpty();
            Check(schema, "{}").Should().Equal("k: required");
        }

        [Test]
        public void Optional_absent_passes_and_optional_wrong_type_fails()
        {
            Check("{\"k\":\"number?\"}", "{}").Should().BeEmpty();
            Check("{\"k\":\"number?\"}", "{\"k\":\"x\"}").Should().Equal("k: expected number");
        }

        [Test]
        public void Extra_fields_are_accepted()
        {
            Check("{\"a\":\"string\"}", "{\"a\":\"x\",\"zzz\":1}").Should().BeEmpty();
        }

        [Test]
        public void Multiple_violations_keep_field_order()
        {
            Check("{\"b\":\"string\",\"a\":\"number\"}", "{}").Should().Equal("b: required", "a: required");
        }

        [TestCase(null)]
        [TestCase("  ")]
        public void Null_or_blank_input_is_treated_as_empty_object(string? input)
        {
            Check("{\"a\":\"string\"}", input).Should().Equal("a: required");
            Check("{\"a\":\"string?\"}", input).Should().BeEmpty();
        }

        [TestCase("abc")]
        [TestCase("[1]")]
        [TestCase("{bad")]
        [TestCase("42")]
        public void Input_that_is_not_a_json_object_is_not_validated(string input)
        {
            Check("{\"a\":\"string\"}", input).Should().BeEmpty();
        }

        [Test]
        public void Empty_field_list_never_reports()
        {
            InputSchemaValidator.Validate([], "{\"x\":1}").Should().BeEmpty();
        }
    }
}

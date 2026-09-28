using IntegrationPlatform.Infrastructure.Services.Contracts;

namespace IntegrationPlatform.Testing.Infrastructure.Contracts
{
    [TestFixture]
    public sealed class ContractSchemaParserTests
    {
        private static ContractField ParseSingle(string description)
        {
            string schema = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string> { ["campo"] = description });

            IReadOnlyList<ContractField> fields = ContractSchemaParser.Parse(schema);

            fields.Should().ContainSingle();
            return fields[0];
        }

        [TestCase("string", ContractFieldType.String, false)]
        [TestCase("string?", ContractFieldType.String, true)]
        [TestCase("string date?", ContractFieldType.String, true)]
        [TestCase("string (obrigatorio)", ContractFieldType.String, false)]
        [TestCase("string (E.164, obrigatorio)", ContractFieldType.String, false)]
        [TestCase("number", ContractFieldType.Number, false)]
        [TestCase("number?", ContractFieldType.Number, true)]
        [TestCase("bool", ContractFieldType.Boolean, false)]
        [TestCase("boolean", ContractFieldType.Boolean, false)]
        [TestCase("BOOL", ContractFieldType.Boolean, false)]
        [TestCase("string[]", ContractFieldType.Array, false)]
        [TestCase("string[]?", ContractFieldType.Array, true)]
        [TestCase("[{name, email}]", ContractFieldType.Array, false)]
        [TestCase("{email, name}", ContractFieldType.Object, false)]
        [TestCase("boleto", ContractFieldType.Unknown, false)]
        [TestCase("application/pdf", ContractFieldType.Unknown, false)]
        [TestCase("", ContractFieldType.Unknown, false)]
        public void Parse_classifies_type_and_optionality(string description, ContractFieldType expectedType, bool expectedOptional)
        {
            ContractField field = ParseSingle(description);

            field.Type.Should().Be(expectedType);
            field.IsOptional.Should().Be(expectedOptional);
        }

        [Test]
        public void Parse_reads_enum_values()
        {
            ContractField field = ParseSingle("Cpf|Cnpj|Email|Phone|Random");

            field.Type.Should().Be(ContractFieldType.Enum);
            field.EnumValues.Should().Equal("Cpf", "Cnpj", "Email", "Phone", "Random");
        }

        [Test]
        public void Parse_enum_with_optional_marker()
        {
            ContractField field = ParseSingle("Cpf|Cnpj?");

            field.Type.Should().Be(ContractFieldType.Enum);
            field.IsOptional.Should().BeTrue();
            field.EnumValues.Should().Equal("Cpf", "Cnpj");
        }

        [Test]
        public void Parse_keeps_original_description()
        {
            ParseSingle("string date?").Description.Should().Be("string date?");
        }

        [Test]
        public void Parse_non_string_values_are_unknown_and_required()
        {
            IReadOnlyList<ContractField> fields = ContractSchemaParser.Parse("{\"a\":{\"b\":\"string\"},\"c\":5}");

            fields.Should().HaveCount(2);
            fields.Should().OnlyContain(field => field.Type == ContractFieldType.Unknown && !field.IsOptional);
        }

        [Test]
        public void Parse_preserves_field_order()
        {
            ContractSchemaParser.Parse("{\"z\":\"string\",\"a\":\"number\"}").Select(field => field.Name).Should().Equal("z", "a");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("not json")]
        [TestCase("[1,2]")]
        [TestCase("\"x\"")]
        public void Parse_returns_no_fields_for_unusable_schema(string? schema)
        {
            ContractSchemaParser.Parse(schema).Should().BeEmpty();
        }
    }
}

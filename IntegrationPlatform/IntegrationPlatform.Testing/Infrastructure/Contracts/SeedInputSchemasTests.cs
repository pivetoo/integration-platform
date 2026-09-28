using System.Text.Json;
using IntegrationPlatform.Infrastructure.Services.Contracts;

namespace IntegrationPlatform.Testing.Infrastructure.Contracts
{
    // Os testes de integracao nao enxergam os contratos das migrations (o SetUp trunca servicecontract),
    // entao a garantia de que o parser aguenta os schemas reais e uma fixture de texto.
    [TestFixture]
    public sealed class SeedInputSchemasTests
    {
        // Copia dos inputSchema das chamadas SeedContract em IntegrationPlatform.Infrastructure/Migrations/Integrations (2026-09-28).
        private static readonly string[] Schemas =
        [
            """
            { "campaignDocumentId": "number (obrigatorio)", "title": "string (obrigatorio)", "documentBase64": "string PDF base64 (obrigatorio)", "documentName": "string", "mimeType": "application/pdf", "message": "string?", "callbackToken": "string (obrigatorio)", "signers": "[{ role, name, email, documentNumber }] (obrigatorio)" }
            """,
            """
            { "payableId": "number?", "idempotencyKey": "string", "callbackToken": "string", "identificationField": "string linha digitavel", "amount": "number?", "dueDate": "string date?", "description": "string?", "scheduleDate": "string date?" }
            """,
            """
            { "payableId": "number?", "idempotencyKey": "string", "callbackToken": "string", "pixPayload": "string copia-e-cola", "amount": "number?", "description": "string?", "scheduleDate": "string date?" }
            """,
            """
            { "creatorPaymentId": "number", "idempotencyKey": "string", "callbackToken": "string", "netAmount": "number", "pixKey": "string", "pixKeyType": "Cpf|Cnpj|Email|Phone|Random", "creatorName": "string", "creatorDocument": "string", "description": "string?" }
            """,
            """
            { "chargeId": "string (obrigatorio)", "financialEntryId": "number (obrigatorio)", "amount": "number?", "dueAt": "string date?", "fineValue": "number?", "interestMonthlyPercent": "number?", "discountValue": "number?", "discountUntil": "string date?", "callbackToken": "string" }
            """,
            """
            { "nossoNumero": "string", "financialEntryId": "number" }
            """,
            """
            { "financialEntryId": "number (obrigatorio)", "amount": "number (obrigatorio)", "dueAt": "string date (obrigatorio)", "method": "boleto | pix", "payerName": "string", "payerDocument": "string CPF/CNPJ", "payerCep": "string", "payerStreet": "string", "payerNumber": "string", "payerCity": "string", "payerState": "string", "interestMonthlyPercent": "number?", "callbackToken": "string" }
            """,
            """
            { "chargeId": "string (obrigatorio)", "financialEntryId": "number", "callbackToken": "string" }
            """,
            """
            { "dia": "string date (dd/MM/yyyy)", "codigoBeneficiario": "string?" }
            """,
            """
            { "to": "string[] (obrigatorio)", "cc": "string[]?", "bcc": "string[]?", "subject": "string (obrigatorio)", "body": "string (obrigatorio)", "isHtml": "bool", "attachments": "[{filename, contentType, contentBase64 | url}]?", "replyTo": "string?", "from": "{email, name}?" }
            """,
            """
            { "to": "string (E.164, obrigatorio)", "channel": "whatsapp | sms (obrigatorio)", "template": "{name, language, variables{}}?", "body": "string?", "attachments": "[{type: image|document|video, url}]?" }
            """
        ];

        private static string BuildSamplePayload(IReadOnlyList<ContractField> fields)
        {
            Dictionary<string, object> payload = [];
            foreach (ContractField field in fields)
            {
                payload[field.Name] = field.Type switch
                {
                    ContractFieldType.Number => 1,
                    ContractFieldType.Boolean => true,
                    ContractFieldType.Array => Array.Empty<object>(),
                    ContractFieldType.Object => new Dictionary<string, object>(),
                    ContractFieldType.Enum => field.EnumValues[0],
                    _ => "x"
                };
            }

            return JsonSerializer.Serialize(payload);
        }

        [Test]
        public void Fixture_is_not_vacuous()
        {
            Schemas.Length.Should().BeGreaterThanOrEqualTo(10);
        }

        [TestCaseSource(nameof(Schemas))]
        public void Real_schema_parses_into_fields(string schema)
        {
            IReadOnlyList<ContractField> fields = ContractSchemaParser.Parse(schema);

            fields.Should().NotBeEmpty();
            fields.Should().OnlyContain(field => field.Name != string.Empty);
        }

        [TestCaseSource(nameof(Schemas))]
        public void Sample_payload_built_from_the_schema_passes_validation(string schema)
        {
            IReadOnlyList<ContractField> fields = ContractSchemaParser.Parse(schema);

            InputSchemaValidator.Validate(fields, BuildSamplePayload(fields)).Should().BeEmpty();
        }

        [TestCaseSource(nameof(Schemas))]
        public void Payload_missing_a_required_field_is_rejected(string schema)
        {
            IReadOnlyList<ContractField> fields = ContractSchemaParser.Parse(schema);
            if (fields.All(field => field.IsOptional))
            {
                Assert.Pass("schema has only optional fields");
            }

            InputSchemaValidator.Validate(fields, "{}").Should().Contain(violation => violation.EndsWith(": required"));
        }
    }
}

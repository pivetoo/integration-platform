using IntegrationPlatform.Infrastructure.Services.ExecutionEngine;

namespace IntegrationPlatform.Testing.Infrastructure.ExecutionEngine
{
    [TestFixture]
    public sealed class SqlScriptParameterizerTests
    {
        private static Dictionary<string, object> Data(params (string key, object value)[] items)
        {
            Dictionary<string, object> data = new();
            foreach ((string key, object value) in items)
            {
                data[key] = value;
            }

            return data;
        }

        private static Dictionary<string, string> Attrs(params (string key, string value)[] items)
        {
            Dictionary<string, string> data = new();
            foreach ((string key, string value) in items)
            {
                data[key] = value;
            }

            return data;
        }

        [Test]
        public void Build_should_bind_quoted_payload_value_as_parameter_keeping_sql_static()
        {
            Dictionary<string, object> payload = Data(("email", "x'; DROP TABLE users; --"));
            string script = "SELECT * FROM users WHERE email = '{{ email }}'";

            ParameterizedSql result = SqlScriptParameterizer.Build(script, Data(), payload, Attrs());

            result.CommandText.Should().Be("SELECT * FROM users WHERE email = @p0");
            result.CommandText.Should().NotContain("DROP TABLE");
            result.Parameters.Should().ContainSingle();
            result.Parameters[0].Name.Should().Be("@p0");
            result.Parameters[0].Value.Should().Be("x'; DROP TABLE users; --");
        }

        [Test]
        public void Build_should_parameterize_unquoted_token_and_preserve_value_type()
        {
            Dictionary<string, object> payload = Data(("id", 42L));
            string script = "SELECT * FROM orders WHERE id = {{ id }}";

            ParameterizedSql result = SqlScriptParameterizer.Build(script, Data(), payload, Attrs());

            result.CommandText.Should().Be("SELECT * FROM orders WHERE id = @p0");
            result.Parameters[0].Value.Should().Be(42L);
        }

        [Test]
        public void Build_should_reuse_one_parameter_for_a_repeated_token()
        {
            Dictionary<string, object> payload = Data(("status", "open"));
            string script = "UPDATE t SET a = '{{ status }}' WHERE b = '{{ status }}'";

            ParameterizedSql result = SqlScriptParameterizer.Build(script, Data(), payload, Attrs());

            result.CommandText.Should().Be("UPDATE t SET a = @p0 WHERE b = @p0");
            result.Parameters.Should().ContainSingle();
        }

        [Test]
        public void Build_should_number_distinct_tokens_in_order()
        {
            Dictionary<string, object> payload = Data(("a", "1"), ("b", "2"));
            string script = "SELECT * FROM t WHERE x = '{{ a }}' AND y = '{{ b }}'";

            ParameterizedSql result = SqlScriptParameterizer.Build(script, Data(), payload, Attrs());

            result.CommandText.Should().Be("SELECT * FROM t WHERE x = @p0 AND y = @p1");
            result.Parameters.Select(parameter => parameter.Name).Should().Equal("@p0", "@p1");
            result.Parameters.Select(parameter => parameter.Value).Should().Equal("1", "2");
        }

        [Test]
        public void Build_should_leave_static_sql_untouched_when_there_are_no_tokens()
        {
            string script = "SELECT 1";

            ParameterizedSql result = SqlScriptParameterizer.Build(script, Data(), Data(), Attrs());

            result.CommandText.Should().Be("SELECT 1");
            result.Parameters.Should().BeEmpty();
        }

        [Test]
        public void Build_should_prefer_step_variable_over_payload()
        {
            Dictionary<string, object> stepVariables = Data(("name", "from-step"));
            Dictionary<string, object> payload = Data(("name", "from-payload"));
            string script = "SELECT * FROM t WHERE name = '{{ name }}'";

            ParameterizedSql result = SqlScriptParameterizer.Build(script, stepVariables, payload, Attrs());

            result.Parameters[0].Value.Should().Be("from-step");
        }

        [Test]
        public void Build_should_resolve_connector_attribute_when_not_in_step_or_payload()
        {
            Dictionary<string, string> attributes = Attrs(("schema_key", "tenant-key"));
            string script = "SELECT * FROM t WHERE k = '{{ schema_key }}'";

            ParameterizedSql result = SqlScriptParameterizer.Build(script, Data(), Data(), attributes);

            result.Parameters[0].Value.Should().Be("tenant-key");
        }

        [Test]
        public void Build_should_serialize_json_filtered_token_and_keep_cast()
        {
            Dictionary<string, object> payload = Data(("doc", Data(("a", 1L))));
            string script = "INSERT INTO t(data) VALUES ({{ doc | json }}::jsonb)";

            ParameterizedSql result = SqlScriptParameterizer.Build(script, Data(), payload, Attrs());

            result.CommandText.Should().Be("INSERT INTO t(data) VALUES (@p0::jsonb)");
            result.Parameters[0].Value.Should().Be("{\"a\":1}");
        }

        [Test]
        public void Build_should_bind_empty_string_for_unresolved_token()
        {
            string script = "SELECT * FROM t WHERE email = '{{ missing }}'";

            ParameterizedSql result = SqlScriptParameterizer.Build(script, Data(), Data(), Attrs());

            result.CommandText.Should().Be("SELECT * FROM t WHERE email = @p0");
            result.Parameters[0].Value.Should().Be(string.Empty);
        }

        [Test]
        public void Build_should_not_re_expand_template_syntax_present_in_a_value()
        {
            Dictionary<string, object> payload = Data(
                ("outer", "'{{ inner }}'"),
                ("inner", "INJECTED"));
            string script = "SELECT * FROM t WHERE a = '{{ outer }}'";

            ParameterizedSql result = SqlScriptParameterizer.Build(script, Data(), payload, Attrs());

            result.CommandText.Should().Be("SELECT * FROM t WHERE a = @p0");
            result.Parameters.Should().ContainSingle();
            result.Parameters[0].Value.Should().Be("'{{ inner }}'");
        }
    }
}

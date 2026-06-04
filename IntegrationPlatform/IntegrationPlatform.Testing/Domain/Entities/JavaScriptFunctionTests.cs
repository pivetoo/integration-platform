using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class JavaScriptFunctionTests
    {
        [Test]
        public void Constructor_trims_name_and_sets_fields()
        {
            JavaScriptFunction function = new(name: "  Transform  ", code: "return payload;", description: "  d  ");

            function.Name.Should().Be("Transform");
            function.Code.Should().Be("return payload;");
            function.Description.Should().Be("d");
        }

        [TestCase("", "code")]
        [TestCase("   ", "code")]
        [TestCase("name", "")]
        [TestCase("name", "   ")]
        public void Constructor_with_blank_name_or_code_throws(string name, string code)
        {
            Action act = () => new JavaScriptFunction(name, code);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Update_changes_fields()
        {
            JavaScriptFunction function = new("old", "return 1;");

            function.Update("  new  ", "return 2;", "desc");

            function.Name.Should().Be("new");
            function.Code.Should().Be("return 2;");
        }
    }
}

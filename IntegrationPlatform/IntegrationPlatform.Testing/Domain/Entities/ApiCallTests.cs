using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class ApiCallTests
    {
        [Test]
        public void Constructor_trims_and_sets_fields()
        {
            ApiCall call = new(name: "  Charge  ", HttpMethodType.Post, url: "  https://api/x  ", description: "  d  ", headersTemplate: "{}", bodyTemplate: "{}");

            call.Name.Should().Be("Charge");
            call.Method.Should().Be(HttpMethodType.Post);
            call.Url.Should().Be("https://api/x");
            call.Description.Should().Be("d");
            call.HeadersTemplate.Should().Be("{}");
            call.BodyTemplate.Should().Be("{}");
        }

        [TestCase("", "https://api/x")]
        [TestCase("   ", "https://api/x")]
        [TestCase("name", "")]
        [TestCase("name", "   ")]
        public void Constructor_with_blank_name_or_url_throws(string name, string url)
        {
            Action act = () => new ApiCall(name, HttpMethodType.Get, url);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Update_changes_fields()
        {
            ApiCall call = new("old", HttpMethodType.Get, "https://api/a");

            call.Update("  new  ", HttpMethodType.Delete, "  https://api/b  ", "desc", "{\"h\":1}", "{\"b\":1}");

            call.Name.Should().Be("new");
            call.Method.Should().Be(HttpMethodType.Delete);
            call.Url.Should().Be("https://api/b");
        }
    }
}

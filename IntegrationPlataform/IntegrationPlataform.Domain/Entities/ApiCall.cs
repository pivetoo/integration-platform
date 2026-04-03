using Archon.Core.Entities;
using IntegrationPlataform.Domain.ValueObjects;

namespace IntegrationPlataform.Domain.Entities
{
    public class ApiCall : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public HttpMethodType Method { get; private set; }

        public string Url { get; private set; } = string.Empty;

        public string? HeadersTemplate { get; private set; }

        public string? BodyTemplate { get; private set; }

        private ApiCall()
        {
        }

        public ApiCall(string name, HttpMethodType method, string url, string? description = null, string? headersTemplate = null, string? bodyTemplate = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(url);

            Name = name.Trim();
            Description = description?.Trim();
            Method = method;
            Url = url.Trim();
            HeadersTemplate = headersTemplate;
            BodyTemplate = bodyTemplate;
        }

        public void Update(string name, HttpMethodType method, string url, string? description, string? headersTemplate, string? bodyTemplate)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(url);

            Name = name.Trim();
            Description = description?.Trim();
            Method = method;
            Url = url.Trim();
            HeadersTemplate = headersTemplate;
            BodyTemplate = bodyTemplate;
        }
    }
}

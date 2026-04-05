using IntegrationPlataform.Domain.Entities;
using System.Linq.Expressions;

namespace IntegrationPlataform.Api.Contracts.ApiCalls
{
    public sealed class ApiCallContract
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public int Method { get; init; }

        public string Url { get; init; } = string.Empty;

        public string? HeadersTemplate { get; init; }

        public string? BodyTemplate { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<ApiCall, ApiCallContract>> Projection => item => new ApiCallContract
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Method = (int)item.Method,
            Url = item.Url,
            HeadersTemplate = item.HeadersTemplate,
            BodyTemplate = item.BodyTemplate,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}

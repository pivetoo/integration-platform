using Archon.Core.Entities;

namespace IntegrationPlatform.Domain.Entities
{
    public class JavaScriptFunction : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public string Code { get; private set; } = string.Empty;

        private JavaScriptFunction()
        {
        }

        public JavaScriptFunction(string name, string code, string? description = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(code);

            Name = name.Trim();
            Description = description?.Trim();
            Code = code;
        }

        public void Update(string name, string code, string? description)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(code);

            Name = name.Trim();
            Description = description?.Trim();
            Code = code;
        }
    }
}

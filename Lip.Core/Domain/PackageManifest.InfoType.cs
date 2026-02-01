using Flurl;
using Lip.Core.JsonConverters;
using System.Text.Json.Serialization;

namespace Lip.Core;

public partial record PackageManifest
{
    public record InfoType
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = "";

        [JsonPropertyName("description")]
        public string Description { get; init; } = "";

        [JsonPropertyName("tags")]
        public List<string> Tags
        {
            get;
            init
            {
                foreach (var tag in value)
                {
                    if (!IsValidTag(tag))
                        throw new SchemaViolationException("info.tags[]", $"Invalid tag '{tag}'.");
                }
                field = value;
            }
        } = [];

        [JsonPropertyName("avatar_url")]
        [JsonConverter(typeof(UrlConverter))]
        public Url? AvatarUrl { get; init; }
    }
}
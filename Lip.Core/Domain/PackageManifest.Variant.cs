using DotNet.Globbing;
using Semver;
using System.Text.Json.Serialization;

namespace Lip.Core;

public partial record PackageManifest
{
    public record Variant
    {
        [JsonPropertyName("label")]
        public string Label { get; init; } = "";

        [JsonPropertyName("platform")]
        public string Platform { get; init; } = "";

        [JsonPropertyName("dependencies")]
        public Dictionary<PackageIdentifier, SemVersionRange> Dependencies { get; init; } = [];

        [JsonPropertyName("assets")]
        public List<Asset> Assets { get; init; } = [];

        [JsonPropertyName("preserve_files")]
        public List<string> PreserveFiles
        {
            get;
            init
            {
                foreach (var file in value)
                {
                    if (!IsValidPlacementDest(file))
                        throw new SchemaViolationException("variants[].preserve_files[]", $"Invalid preserve file path '{file}'.");
                }
                field = value;
            }
        } = [];

        [JsonPropertyName("remove_files")]
        public List<string> RemoveFiles
        {
            get;
            init
            {
                foreach (var file in value)
                {
                    if (!IsValidPlacementDest(file))
                        throw new SchemaViolationException("variants[].remove_files[]", $"Invalid remove file path '{file}'.");
                }
                field = value;
            }
        } = [];

        [JsonPropertyName("scripts")]
        public ScriptsType Scripts { get; init; } = new();

        public bool Match(string targetLabel, string targetPlatform)
        {
            string label = Label ?? "";
            string platform = Platform ?? "";

            if (label != targetLabel)
            {
                if (label == string.Empty)
                    return false;
                if (!Glob.Parse(label).IsMatch(targetLabel))
                    return false;
            }

            if (platform != targetPlatform)
            {
                if (platform == string.Empty)
                    return false;
                if (!Glob.Parse(platform).IsMatch(targetPlatform))
                    return false;
            }

            return true;
        }
    }
}
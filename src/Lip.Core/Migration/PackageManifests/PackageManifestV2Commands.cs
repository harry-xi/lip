using System.Text.Json.Serialization;

namespace Lip.Core.Migration.PackageManifests;

public record PackageManifestV2Commands {
  [JsonPropertyName("pre_install")]
  public List<string>? PreInstall { get; set; }

  [JsonPropertyName("post_install")]
  public List<string>? PostInstall { get; set; }

  [JsonPropertyName("pre_uninstall")]
  public List<string>? PreUninstall { get; set; }

  [JsonPropertyName("post_uninstall")]
  public List<string>? PostUninstall { get; set; }
}
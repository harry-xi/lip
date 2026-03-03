using System.Text.Json;
using Lip.Core.Entities;
using Semver;

namespace Lip.Core.Tests.Json;

public class DependencyDictJsonConverterTests {
  private static readonly JsonSerializerOptions _options = new() {
    Converters = { new Core.Json.PackageIdToSemVersionRangeDictionary() }
  };

  [Fact]
  public void Read_ValidDict_ReturnsDictionary() {
    string json = "{\"github.com/a/b#main\": \"1.0.0\"}";

    Dictionary<PackageId, SemVersionRange>? result = JsonSerializer.Deserialize<Dictionary<PackageId, SemVersionRange>>(json, _options);

    Assert.NotNull(result);
    Assert.Single(result);
    KeyValuePair<PackageId, SemVersionRange> kvp = result.First();
    Assert.Equal("github.com/a/b", kvp.Key.Path);
    Assert.Equal("main", kvp.Key.Variant);
    Assert.True(kvp.Value.Contains(new SemVersion(1, 0, 0)));
  }

  [Fact]
  public void Write_ValidDict_WritesJson() {
    Dictionary<PackageId, SemVersionRange> dict = new()
    {
            { new PackageId("github.com/a/b", "main"), SemVersionRange.Parse("1.0.0") }
        };

    string result = JsonSerializer.Serialize(dict, _options);

    Assert.Contains("github.com/a/b#main", result);
    Assert.Contains("1.0.0", result);
  }
}
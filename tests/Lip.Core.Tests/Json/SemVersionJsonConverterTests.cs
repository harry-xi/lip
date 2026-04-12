using System.Text.Json;
using Semver;

namespace Lip.Core.Tests.Json;

public class SemVersionJsonConverterTests {
  private static readonly JsonSerializerOptions _options = new() {
    Converters = { new Core.Json.SemVersionJsonConverter() }
  };

  [Fact]
  public void Read_ValidVersion_ReturnsSemVersion() {
    string json = "\"1.2.3\"";
    SemVersion? result = JsonSerializer.Deserialize<SemVersion>(json, _options);
    Assert.NotNull(result);
    Assert.Equal(1, result.Major);
    Assert.Equal(2, result.Minor);
    Assert.Equal(3, result.Patch);
  }

  [Fact]
  public void Write_ValidVersion_WritesString() {
    SemVersion version = new(1, 2, 3);
    string result = JsonSerializer.Serialize(version, _options);
    Assert.Equal("\"1.2.3\"", result);
  }
}

using System.Text.Json;
using DotNet.Globbing;

namespace Lip.Core.Tests.Json;

public class GlobListJsonConverterTests {
  private static readonly JsonSerializerOptions _options = new() {
    Converters = { new Core.Json.GlobListJsonConverter() }
  };

  [Fact]
  public void Read_ValidGlobArray_ReturnsGlobList() {
    string json = "[\"*.txt\", \"**/*.cs\"]";
    List<Glob>? result = JsonSerializer.Deserialize<List<Glob>>(json, _options);
    Assert.NotNull(result);
    Assert.Equal(2, result.Count);
    Assert.True(result[0].IsMatch("file.txt"));
    Assert.True(result[1].IsMatch("src/file.cs"));
  }

  [Fact]
  public void Write_ValidGlobList_WritesArray() {
    List<Glob> globs = [Glob.Parse("*.txt"), Glob.Parse("**/*.cs")];
    string result = JsonSerializer.Serialize(globs, _options);
    Assert.Equal("[\"*.txt\",\"**/*.cs\"]", result);
  }
}
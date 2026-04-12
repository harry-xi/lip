using System.Text.Json;
using Flurl;

namespace Lip.Core.Tests.Json;

public class UrlJsonConverterTests {
  private static readonly JsonSerializerOptions _options = new() {
    Converters = { new Core.Json.UrlJsonConverter() }
  };

  [Fact]
  public void Read_ValidUrl_ReturnsUrl() {
    string json = "\"https://example.com/path\"";
    Url? result = JsonSerializer.Deserialize<Flurl.Url>(json, _options);
    Assert.NotNull(result);
    Assert.Equal("https://example.com/path", result.ToString());
  }

  [Fact]
  public void Write_ValidUrl_WritesString() {
    Url url = new("https://example.com/path");
    string result = JsonSerializer.Serialize(url, _options);
    Assert.Equal("\"https://example.com/path\"", result);
  }
}

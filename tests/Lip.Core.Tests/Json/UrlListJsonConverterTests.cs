using System.Text.Json;
using Flurl;

namespace Lip.Core.Tests.Json;

public class UrlListJsonConverterTests {
  private static readonly JsonSerializerOptions _options = new() {
    Converters = { new Core.Json.UrlListJsonConverter() }
  };

  [Fact]
  public void Read_ValidUrlArray_ReturnsUrlList() {
    string json = "[\"https://example.com/1\", \"https://example.com/2\"]";
    List<Url>? result = JsonSerializer.Deserialize<List<Url>>(json, _options);
    Assert.NotNull(result);
    Assert.Equal(2, result.Count);
    Assert.Equal("https://example.com/1", result[0].ToString());
    Assert.Equal("https://example.com/2", result[1].ToString());
  }

  [Fact]
  public void Write_ValidUrlList_WritesArray() {
    List<Url> urls = [new Url("https://example.com/1"), new Url("https://example.com/2")];
    string result = JsonSerializer.Serialize(urls, _options);
    Assert.Equal("[\"https://example.com/1\",\"https://example.com/2\"]", result);
  }
}

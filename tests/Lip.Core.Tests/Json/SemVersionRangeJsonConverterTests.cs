using Semver;
using System.Text.Json;

namespace Lip.Core.Tests.Json;

public class SemVersionRangeJsonConverterTests
{
    private static readonly JsonSerializerOptions _options = new()
    {
        Converters = { new Core.Json.SemVersionRangeJsonConverter() }
    };

    [Fact]
    public void Read_ValidRange_ReturnsSemVersionRange()
    {
        string json = "\">=1.0.0\"";
        SemVersionRange? result = JsonSerializer.Deserialize<SemVersionRange>(json, _options);
        Assert.NotNull(result);
        Assert.True(result.Contains(new SemVersion(1, 0, 0)));
        Assert.True(result.Contains(new SemVersion(2, 0, 0)));
    }

    [Fact]
    public void Write_ValidRange_WritesString()
    {
        SemVersionRange range = SemVersionRange.Parse(">=1.0.0");
        string result = JsonSerializer.Serialize(range, _options);
        Assert.Contains("1.0.0", result);
    }
}
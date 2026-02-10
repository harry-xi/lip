using DotNet.Globbing;
using Lip.Core.Entities;
using Semver;
using System.Text.Json;

namespace Lip.Core.Tests.Json;

public class JsonConverterTests
{
    private static readonly JsonSerializerOptions _options = new()
    {
        Converters =
        {
            new Core.Json.GlobJsonConverter(),
            new Core.Json.PackageIdJsonConverter(),
            new Core.Json.SemVersionJsonConverter(),
            new Core.Json.SemVersionRangeJsonConverter(),
            new Core.Json.UrlJsonConverter()
        }
    };


    #region GlobJsonConverter

    [Fact]
    public void GlobJsonConverter_Read_ValidGlob_ReturnsGlob()
    {
        var json = "\"*.txt\"";
        var result = JsonSerializer.Deserialize<Glob>(json, _options);
        Assert.NotNull(result);
        Assert.True(result.IsMatch("file.txt"));
    }

    [Fact]
    public void GlobJsonConverter_Write_ValidGlob_WritesString()
    {
        var glob = Glob.Parse("*.txt");
        var result = JsonSerializer.Serialize(glob, _options);
        Assert.Equal("\"*.txt\"", result);
    }

    #endregion

    #region PackageIdJsonConverter

    [Fact]
    public void PackageIdJsonConverter_Read_ValidPackageId_ReturnsPackageId()
    {
        var json = "\"github.com/user/repo#variant\"";
        var result = JsonSerializer.Deserialize<PackageId>(json, _options);
        Assert.NotNull(result);
        Assert.Equal("github.com/user/repo", result.Path);
        Assert.Equal("variant", result.Variant);
    }

    [Fact]
    public void PackageIdJsonConverter_Write_ValidPackageId_WritesString()
    {
        var packageId = new PackageId("github.com/user/repo", "variant");
        var result = JsonSerializer.Serialize(packageId, _options);
        Assert.Equal("\"github.com/user/repo#variant\"", result);
    }

    [Fact]
    public void PackageIdJsonConverter_ReadAsPropertyName_Works()
    {
        var json = "{\"github.com/user/repo\": \"value\"}";
        var result = JsonSerializer.Deserialize<Dictionary<PackageId, string>>(json, _options);
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("github.com/user/repo", result.Keys.First().Path);
    }

    [Fact]
    public void PackageIdJsonConverter_WriteAsPropertyName_Works()
    {
        var dict = new Dictionary<PackageId, string>
        {
            { new PackageId("github.com/user/repo", ""), "value" }
        };
        var result = JsonSerializer.Serialize(dict, _options);
        Assert.Contains("github.com/user/repo", result);
    }

    #endregion

    #region SemVersionJsonConverter

    [Fact]
    public void SemVersionJsonConverter_Read_ValidVersion_ReturnsSemVersion()
    {
        var json = "\"1.2.3\"";
        var result = JsonSerializer.Deserialize<SemVersion>(json, _options);
        Assert.NotNull(result);
        Assert.Equal(1, result.Major);
        Assert.Equal(2, result.Minor);
        Assert.Equal(3, result.Patch);
    }

    [Fact]
    public void SemVersionJsonConverter_Write_ValidVersion_WritesString()
    {
        var version = new SemVersion(1, 2, 3);
        var result = JsonSerializer.Serialize(version, _options);
        Assert.Equal("\"1.2.3\"", result);
    }

    #endregion

    #region SemVersionRangeJsonConverter

    [Fact]
    public void SemVersionRangeJsonConverter_Read_ValidRange_ReturnsSemVersionRange()
    {
        var json = "\">=1.0.0\"";
        var result = JsonSerializer.Deserialize<SemVersionRange>(json, _options);
        Assert.NotNull(result);
        Assert.True(result.Contains(new SemVersion(1, 0, 0)));
        Assert.True(result.Contains(new SemVersion(2, 0, 0)));
    }

    [Fact]
    public void SemVersionRangeJsonConverter_Write_ValidRange_WritesString()
    {
        var range = SemVersionRange.Parse(">=1.0.0");
        var result = JsonSerializer.Serialize(range, _options);
        Assert.Contains("1.0.0", result);
    }

    #endregion

    #region UrlJsonConverter

    [Fact]
    public void UrlJsonConverter_Read_ValidUrl_ReturnsUrl()
    {
        var json = "\"https://example.com/path\"";
        var result = JsonSerializer.Deserialize<Flurl.Url>(json, _options);
        Assert.NotNull(result);
        Assert.Equal("https://example.com/path", result.ToString());
    }

    [Fact]
    public void UrlJsonConverter_Write_ValidUrl_WritesString()
    {
        var url = new Flurl.Url("https://example.com/path");
        var result = JsonSerializer.Serialize(url, _options);
        Assert.Equal("\"https://example.com/path\"", result);
    }

    #endregion

    #region DependencyDictJsonConverter

    [Fact]
    public void DependencyDictJsonConverter_Read_ValidDict_ReturnsDictionary()
    {
        var json = "{\"github.com/a/b#main\": \"1.0.0\"}";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new Core.Json.DependencyDictJsonConverter());

        var result = JsonSerializer.Deserialize<Dictionary<PackageId, SemVersionRange>>(json, options);

        Assert.NotNull(result);
        Assert.Single(result);
        var kvp = result.First();
        Assert.Equal("github.com/a/b", kvp.Key.Path);
        Assert.Equal("main", kvp.Key.Variant);
        Assert.True(kvp.Value.Contains(new SemVersion(1, 0, 0)));
    }

    [Fact]
    public void DependencyDictJsonConverter_Write_ValidDict_WritesJson()
    {
        var dict = new Dictionary<PackageId, SemVersionRange>
        {
            { new PackageId("github.com/a/b", "main"), SemVersionRange.Parse("1.0.0") }
        };
        var options = new JsonSerializerOptions();
        options.Converters.Add(new Core.Json.DependencyDictJsonConverter());

        var result = JsonSerializer.Serialize(dict, options);

        Assert.Contains("github.com/a/b#main", result);
        Assert.Contains("1.0.0", result);
    }

    #endregion
}
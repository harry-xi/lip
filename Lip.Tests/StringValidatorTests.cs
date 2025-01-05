namespace Lip.Tests;

public class StringValidatorTests
{
    [Theory]
    [InlineData("script")]
    [InlineData("script_name")]
    public void IsScriptNameValid_CommonInput_Pass(string scriptName)
    {
        Assert.True(StringValidator.IsScriptNameValid(scriptName));
    }

    [Theory]
    [InlineData("script-name")]
    [InlineData("script name")]
    [InlineData("script_name!")]
    public void IsScriptNameValid_InvalidInput_Fail(string scriptName)
    {
        Assert.False(StringValidator.IsScriptNameValid(scriptName));
    }

    [Theory]
    [InlineData("tag")]
    [InlineData("tag:subtag")]
    public void IsTagValid_CommonInput_Pass(string tag)
    {
        Assert.True(StringValidator.IsTagValid(tag));
    }

    [Theory]
    [InlineData("tag name")]
    [InlineData("tag!")]
    public void IsTagValid_InvalidInput_Fail(string tag)
    {
        Assert.False(StringValidator.IsTagValid(tag));
    }

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("1.0.0-alpha")]
    public void IsVersionValid_CommonInput_Pass(string version)
    {
        Assert.True(StringValidator.IsVersionValid(version));
    }

    [Theory]
    [InlineData("1.0.0.0")]
    [InlineData("1.0.0-alpha!")]
    public void IsVersionValid_InvalidInput_Fail(string version)
    {
        Assert.False(StringValidator.IsVersionValid(version));
    }
}

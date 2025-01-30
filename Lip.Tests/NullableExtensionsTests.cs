namespace Lip.Tests;

public class NullableExtensionsTests
{
    [Fact]
    public void DefaultIfNull_NullReferenceType_ReturnsDefault()
    {
        // Arrange.
        string? value = null;

        // Act.
        string result = value.DefaultIfNull();

        // Assert.
        Assert.Null(result);
    }

    [Fact]
    public void DefaultIfNull_NonNullReferenceType_ReturnsValue()
    {
        // Arrange.
        string? value = "value";

        // Act.
        string result = value.DefaultIfNull();

        // Assert.
        Assert.Equal("value", result);
    }

    [Fact]
    public void DefaultIfNull_NullValueType_ReturnsDefault()
    {
        // Arrange.
        int? value = null;

        // Act.
        int result = value.DefaultIfNull();

        // Assert.
        Assert.Equal(default, result);
    }

    [Fact]
    public void DefaultIfNull_NonNullValueType_ReturnsValue()
    {
        // Arrange.
        int? value = 0;

        // Act.
        int result = value.DefaultIfNull();

        // Assert.
        Assert.Equal(0, result);
    }
}

using Lip.Core.SourceProviders;
using Moq;

namespace Lip.Core.Tests.SourceProviders;

public class CompositeSourceProviderTests
{
    [Fact]
    public void Keys_AggregatesAndDistinctsKeys()
    {
        // Arrange
        Mock<ISourceProvider> mockProvider1 = new();
        mockProvider1.Setup(p => p.Keys).Returns(["file1.txt", "common.txt"]);

        Mock<ISourceProvider> mockProvider2 = new();
        mockProvider2.Setup(p => p.Keys).Returns(["file2.txt", "common.txt"]);

        CompositeSourceProvider provider = new([mockProvider1.Object, mockProvider2.Object]);

        // Act
        List<string> keys = provider.Keys.ToList();

        // Assert
        Assert.Equal(3, keys.Count);
        Assert.Contains("file1.txt", keys);
        Assert.Contains("file2.txt", keys);
        Assert.Contains("common.txt", keys);
    }

    [Fact]
    public async Task OpenRead_ReturnsFromFirstProviderWithKey()
    {
        // Arrange
        Mock<ISourceProvider> mockProvider1 = new();
        mockProvider1.Setup(p => p.Keys).Returns(["file1.txt"]);
        mockProvider1.Setup(p => p.OpenRead("file1.txt"))
            .ReturnsAsync(new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content1")));

        Mock<ISourceProvider> mockProvider2 = new();
        mockProvider2.Setup(p => p.Keys).Returns(["file2.txt"]);

        CompositeSourceProvider provider = new([mockProvider1.Object, mockProvider2.Object]);

        // Act
        using Stream stream = await provider.OpenRead("file1.txt");
        using StreamReader reader = new(stream);
        var content = await reader.ReadToEndAsync();

        // Assert
        Assert.Equal("content1", content);
    }

    [Fact]
    public async Task OpenRead_KeyNotFound_ThrowsArgumentException()
    {
        // Arrange
        CompositeSourceProvider provider = new([]);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => provider.OpenRead("missing"));
    }
}
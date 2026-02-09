using Lip.Core.SourceProviders;
using Moq;

namespace Lip.Core.Tests.SourceProviders;

public class CompositeSourceProviderTests
{
    [Fact]
    public void Keys_AggregatesAndDistinctsKeys()
    {
        // Arrange
        var mockProvider1 = new Mock<ISourceProvider>();
        mockProvider1.Setup(p => p.Keys).Returns(["file1.txt", "common.txt"]);

        var mockProvider2 = new Mock<ISourceProvider>();
        mockProvider2.Setup(p => p.Keys).Returns(["file2.txt", "common.txt"]);

        var provider = new CompositeSourceProvider([mockProvider1.Object, mockProvider2.Object]);

        // Act
        var keys = provider.Keys.ToList();

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
        var mockProvider1 = new Mock<ISourceProvider>();
        mockProvider1.Setup(p => p.Keys).Returns(["file1.txt"]);
        mockProvider1.Setup(p => p.OpenRead("file1.txt"))
            .ReturnsAsync(new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content1")));

        var mockProvider2 = new Mock<ISourceProvider>();
        mockProvider2.Setup(p => p.Keys).Returns(["file2.txt"]);

        var provider = new CompositeSourceProvider([mockProvider1.Object, mockProvider2.Object]);

        // Act
        using var stream = await provider.OpenRead("file1.txt");
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        // Assert
        Assert.Equal("content1", content);
    }

    [Fact]
    public async Task OpenRead_KeyNotFound_ThrowsArgumentException()
    {
        // Arrange
        var provider = new CompositeSourceProvider([]);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => provider.OpenRead("missing"));
    }
}
using Semver;
using System.Text;
using System.Text.Json;

namespace Lip.Tests;

public class PackageLockTests
{
    [Fact]
    public void LockType_Constructor_Passes()
    {
        // Arrange.
        PackageLock.Package lockType = new()
        {
            Locked = false,
            Manifest = new()
            {
                FormatVersion = PackageManifest.DefaultFormatVersion,
                FormatUuid = PackageManifest.DefaultFormatUuid,
                ToothPath = "example.com/pkg",
                VersionText = "1.0.0"
            },
            VariantLabel = string.Empty,
            Files = []
        };

        // Act.
        lockType = lockType with { };

        // Assert.
        Assert.False(lockType.Locked);
        Assert.Equal("example.com/pkg", lockType.Manifest.ToothPath);
        Assert.Equal("1.0.0", lockType.Manifest.VersionText);
        Assert.Equal(string.Empty, lockType.VariantLabel);
        Assert.Equal([], lockType.Files);
    }

    [Fact]
    public void LockType_Constructor_InvalidVariantLabel_ThrowsSchemaViolationException()
    {
        // Arrange & Act & Assert
        Assert.Throws<SchemaViolationException>(() => new PackageLock.Package
        {
            Locked = false,
            Manifest = new PackageManifest
            {
                FormatVersion = PackageManifest.DefaultFormatVersion,
                FormatUuid = PackageManifest.DefaultFormatUuid,
                ToothPath = "example.com/pkg",
                VersionText = "1.0.0"
            },
            VariantLabel = "invalid-variant",
            Files = []
        });
    }

    [Fact]
    public void LockType_Specifier_Passes()
    {
        // Arrange.
        PackageLock.Package lockType = new()
        {
            Locked = false,
            Manifest = new()
            {
                FormatVersion = PackageManifest.DefaultFormatVersion,
                FormatUuid = PackageManifest.DefaultFormatUuid,
                ToothPath = "example.com/pkg",
                VersionText = "1.0.0"
            },
            VariantLabel = "variant",
            Files = []
        };

        // Act.
        PackageSpecifier specifier = lockType.Specifier;

        // Assert.
        Assert.Equal("example.com/pkg", specifier.ToothPath);
        Assert.Equal("variant", specifier.VariantLabel);
        Assert.Equal(SemVersion.Parse("1.0.0"), specifier.Version);
    }

    [Fact]
    public void Constructor_ValidValue_Passes()
    {
        // Arrange.
        PackageLock packageLock = new()
        {
            Locks = []
        };

        // Act.
        packageLock = packageLock with { };

        // Assert.
        Assert.Empty(packageLock.Locks);
    }

    [Fact]
    public void FromBytes_MinimumJsonBytes_Passes()
    {
        // Arrange
        byte[] bytes = Encoding.UTF8.GetBytes($$"""
            {
                "format_version": {{PackageLock.DefaultFormatVersion}},
                "format_uuid": "{{PackageLock.DefaultFormatUuid}}",
                "packages": []
            }
            """);

        // Act
        var packageLock = PackageLock.FromJsonBytes(bytes);

        // Assert
        Assert.Empty(packageLock.Locks);
    }

    [Fact]
    public void FromBytes_NullJson_Throws()
    {
        // Arrange
        byte[] bytes = Encoding.UTF8.GetBytes("null");

        // Act & Assert
        Assert.Throws<SchemaViolationException>(() => PackageLock.FromJsonBytes(bytes));
    }

    [Fact]
    public void ToBytes_MinimumJson_Passes()
    {
        // Arrange
        PackageLock packageLock = new()
        {
            Locks = []
        };

        // Act
        byte[] bytes = packageLock.ToJsonBytes();

        // Assert
        Assert.Equal("""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "packages": []
            }
            """.ReplaceLineEndings(), Encoding.UTF8.GetString(bytes).ReplaceLineEndings());
    }
}

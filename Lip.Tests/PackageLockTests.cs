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
        PackageLock.LockType lockType = new()
        {
            Locked = false,
            Package = new()
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
        Assert.Equal("example.com/pkg", lockType.Package.ToothPath);
        Assert.Equal("1.0.0", lockType.Package.VersionText);
        Assert.Equal(string.Empty, lockType.VariantLabel);
        Assert.Equal([],lockType.Files);
    }

    [Fact]
    public void LockType_Constructor_InvalidVariantLabel_ThrowsSchemaViolationException()
    {
        // Arrange & Act & Assert
        Assert.Throws<SchemaViolationException>(() => new PackageLock.LockType
        {
            Locked = false,
            Package = new PackageManifest
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
        PackageLock.LockType lockType = new()
        {
            Locked = false,
            Package = new()
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
            FormatVersion = PackageLock.DefaultFormatVersion,
            FormatUuid = PackageLock.DefaultFormatUuid,
            Locks = []
        };

        // Act.
        packageLock = packageLock with { };

        // Assert.
        Assert.Equal(PackageLock.DefaultFormatVersion, packageLock.FormatVersion);
        Assert.Equal(PackageLock.DefaultFormatUuid, packageLock.FormatUuid);
        Assert.Empty(packageLock.Locks);
    }

    [Fact]
    public void Constructor_InvalidFormatVersion_ThrowsSchemaViolationException()
    {
        // Arrange & Act & Assert
        Assert.Throws<SchemaViolationException>(() => new PackageLock
        {
            FormatVersion = 0,
            FormatUuid = PackageLock.DefaultFormatUuid,
            Locks = []
        });
    }

    [Fact]
    public void Constructor_InvalidFormatUuid_ThrowsSchemaViolationException()
    {
        // Arrange & Act & Assert
        Assert.Throws<SchemaViolationException>(() => new PackageLock
        {
            FormatVersion = PackageLock.DefaultFormatVersion,
            FormatUuid = "invalid-uuid",
            Locks = []
        });
    }

    [Fact]
    public void FromBytes_MinimumJsonBytes_Passes()
    {
        // Arrange
        byte[] bytes = Encoding.UTF8.GetBytes($$"""
            {
                "format_version": {{PackageLock.DefaultFormatVersion}},
                "format_uuid": "{{PackageLock.DefaultFormatUuid}}",
                "locks": []
            }
            """);

        // Act
        var packageLock = PackageLock.FromJsonBytes(bytes);

        // Assert
        Assert.Equal(PackageLock.DefaultFormatVersion, packageLock.FormatVersion);
        Assert.Equal(PackageLock.DefaultFormatUuid, packageLock.FormatUuid);
        Assert.Empty(packageLock.Locks);
    }

    [Fact]
    public void FromBytes_NullJson_Throws()
    {
        // Arrange
        byte[] bytes = Encoding.UTF8.GetBytes("null");

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() => PackageLock.FromJsonBytes(bytes));
        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Fact]
    public void ToBytes_MinimumJson_Passes()
    {
        // Arrange
        PackageLock packageLock = new()
        {
            FormatVersion = PackageLock.DefaultFormatVersion,
            FormatUuid = PackageLock.DefaultFormatUuid,
            Locks = []
        };

        // Act
        byte[] bytes = packageLock.ToJsonBytes();

        // Assert
        Assert.Equal("""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "locks": []
            }
            """.ReplaceLineEndings(), Encoding.UTF8.GetString(bytes).ReplaceLineEndings());
    }
}

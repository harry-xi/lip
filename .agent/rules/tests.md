---
trigger: always_on
---

# Test Rules for Antigravity

## Frameworks & Libraries
- **Test Framework**: [xUnit](https://xunit.net/)
- **Mocking**: [Moq](https://github.com/moq/moq4)
- **FileSystem Mocking**: [System.IO.Abstractions.TestingHelpers](https://github.com/TestableIO/System.IO.Abstractions)
- **Assertions**: xUnit `Assert` class

## Naming Conventions
- **Test Classes**: Must end with `Tests` (e.g., `DependencySolverTests`).
- **Test Methods**: Should follow the pattern `MethodName_StateUnderOrScenario_ExpectedBehavior`.
  - Example: `GetUnnecessaryPackages_ReturnsCorrectPackages`
  - Example: `Init_WithDefaultValues_Passes`
  - Example: `Install_ManifestNotFound_ThrowsInvalidOperationException`

## Structure (AAA Pattern)
Tests should be structured using the **Arrange, Act, Assert** pattern, often marked with comments:

```csharp
[Fact]
public void Method_Scenario_Result()
{
    // Arrange.
    var setup = "value";

    // Act.
    var result = SystemUnderTest.Action(setup);

    // Assert.
    Assert.Equal("expected", result);
}
```

## Mocking & Dependencies
- **Interfaces**: Mock all external dependencies (e.g., `IContext`, `IPackageManager`, `IGit`, `IDownloader`, `ILogger`) using `Moq`.
- **FileSystem**: **NEVER** use real file system paths or operations in unit tests. Use `MockFileSystem` and `MockFileData`/`MockDirectoryData`.
  - Use `OperatingSystem.IsWindows()` checks if path separators matter, or `Path.Join` to be cross-platform safe even in mocks if needed.
  - Initialize `MockFileSystem` with a dictionary of files for "Arrange".

## Async Tests
- Use `async Task` for asynchronous test methods.
- Use `await` for the Act phase if the method under test is async.

## Data-Driven Tests
- Use `[Theory]` and `[InlineData]` for testing multiple input scenarios for the same method.

```csharp
[Theory]
[InlineData("input1", true)]
[InlineData("input2", false)]
public void Method_Input_ReturnsExpected(string input, bool expected)
{
    // ...
}
```

## Best Practices
- **Isolation**: Tests must be independent of each other.
- **Readability**: Use helper methods (e.g., `MakePackage`) to keep tests clean and focused on the assertion logic if setup is complex.
- **Coverage**: Aim to cover both success paths and failure paths (e.g., exceptions using `Assert.ThrowsAsync`).

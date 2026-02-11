---
trigger: model_decision
description: when writing tests
---

# Test Rules for Antigravity

When writing unit tests for the `Lip.Core` project, adhere to the following rules and patterns:

## Frameworks & Libraries
- **Test Framework**: Use **xUnit** (`[Fact]`).
- **Mocking**: Use **Moq** (`Mock<T>`, `It.IsAny<T>`, `Verify`).
- **Assertions**: Use **xUnit Assert** (`Assert.Equal`, `Assert.ThrowsAsync`, `Assert.Contains`).
- **SemVer**: Use the `Semver` library for versioning (`SemVersion`, `SemVersionRange`).

## Naming Conventions
- **Test Classes**: Name test classes after the class being tested, suffixed with `Tests` (e.g., `InstallService` -> `InstallServiceTests`).
- **Test Methods**: Use the `MethodName_StateUnder_ExpectedBehavior` pattern (e.g., `InstallPackages_NoDependencies_InstallsArtifacts`).
  - `MethodName`: The method being tested.
  - `StateUnder`: The condition or state essential for the test.
  - `ExpectedBehavior`: The expected result or outcome.

## Structure & Organization
- **Arrange-Act-Assert**: Clearly structure tests with `// Arrange`, `// Act`, and `// Assert` comments.
- **Constructor Setup**: Use the test class constructor to initialize mocks and the system under test (SUT).
  - Declare mocks as `private readonly Mock<IMyDependency> _mockDependency;`.
  - Initialize mocks and the SUT in the constructor.
- **Async Tests**: Use `public async Task` for asynchronous tests.
- **Namespaces**: Use file-scoped namespaces (e.g., `namespace Lip.Core.Tests.Services;`).

## Mocking Best Practices
- **Dependencies**: Mock all external dependencies injected into the SUT.
- **Logger**: Use `Mock<ILogger<T>>` for typed loggers or `Mock<ILogger>` for generic loggers.
- **Setup**: Use `_mockRepo.Setup(x => x.Method(...)).ReturnsAsync(...)` for async method stubs.
- **Verification**: Use `_mockRepo.Verify(x => x.Method(...), Times.Once)` to assert interactions.
- **Argument Matching**: Use `It.IsAny<T>()` or specific expressions like `It.Is<T>(x => x.Prop == val)` for strict matching.

## Example
```csharp
using Lip.Core.Services;
using Moq;
using Xunit;

namespace Lip.Core.Tests.Services;

public class MyServiceTests
{
    private readonly Mock<IDependency> _mockDependency;
    private readonly MyService _service;

    public MyServiceTests()
    {
        _mockDependency = new Mock<IDependency>();
        _service = new MyService(_mockDependency.Object);
    }

    [Fact]
    public async Task DoWork_WhenConditionMet_ReturnsSuccess()
    {
        // Arrange
        _mockDependency.Setup(x => x.GetData()).ReturnsAsync("data");

        // Act
        var result = await _service.DoWork();

        // Assert
        Assert.True(result);
        _mockDependency.Verify(x => x.GetData(), Times.Once);
    }
}
```

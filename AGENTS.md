# AGENTS.md

This file provides guidance for AI coding agents working in the `lip` repository.
**lip** is a general package installer written in C# 14 / .NET 10.0.

## Project Structure

```
lip.sln                          # Visual Studio solution (6 projects)
src/
  Lip.Cli/                       # CLI frontend (Spectre.Console.Cli) → produces `lip`
  Lip.Core/                      # Core library: entities, services, sources, registries
  Lip.Daemon/                    # JSON-RPC daemon (StreamJsonRpc) → produces `lipd`
  Golang.Org.X.Mod/              # C# port of Go module path/semver validation
tests/
  Lip.Core.Tests/                # xUnit tests for Lip.Core
  Golang.Org.X.Mod.Tests/        # xUnit tests for Golang.Org.X.Mod
docs/                            # VitePress documentation site
schemas/                         # JSON schemas for manifests, config, OpenRPC
```

## Build / Lint / Test Commands

```bash
# Build CLI and Daemon
dotnet publish src/Lip.Cli -o bin -r linux-x64 --no-self-contained
dotnet publish src/Lip.Daemon -o bin -r linux-x64 --no-self-contained

# Run all tests
dotnet test

# Run a single test project
dotnet test tests/Lip.Core.Tests
dotnet test tests/Golang.Org.X.Mod.Tests

# Run a single test class
dotnet test tests/Lip.Core.Tests --filter "FullyQualifiedName~CacheServiceTests"

# Run a single test method
dotnet test tests/Lip.Core.Tests --filter "FullyQualifiedName~CacheServiceTests.GetOrCreateDirectory_NonExistent_CreatesAndCallsFactory"

# Check formatting (CI runs this on Windows)
dotnet format --verify-no-changes

# Auto-fix formatting
dotnet format
```

## Code Style Guidelines

### General Principles

- Keep minimum code. Avoid unnecessary abstractions.
- Write comments if and only if very essential or crucial.
- Prefer C# 14 features.
- Use explicit types (`var` is not used; `csharp_style_var_*` = false).
- Nullable reference types are enabled project-wide.

### Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Classes, methods, enums, public fields/properties, namespaces | `PascalCase` | `CacheService`, `GetOrCreateFile` |
| Local variables, parameters | `camelCase` | `safeKey`, `cacheInfo` |
| Private/protected/internal fields and properties | `_camelCase` | `_fileSystem`, `_cacheDirectory` |
| Interfaces | `I` prefix + PascalCase | `ICacheService` |
| Acronyms | Treat as single word | `MyRpc` (not `MyRPC`) |
| Files and directories | `PascalCase` | `CacheService.cs` |

### File Organization

- One core class per file. File name matches the main class name.
- Interfaces and their implementations can share a file (e.g., `ICacheService` + `CacheService` in `CacheService.cs`).
- Block-scoped namespaces in source files. File-scoped namespaces in test files.

### Import Order

- `using` directives go at the top, outside namespaces.
- `System` imports first, then alphabetical.

### Class Member Ordering

1. Nested types, enums, delegates, events
2. Static, const, and readonly fields
3. Fields and properties
4. Constructors and finalizers
5. Methods

Within each group, order by accessibility: public > internal > protected internal > protected > private.

### Modifier Order

```
public protected internal private new abstract virtual override sealed static readonly extern unsafe volatile async
```

### Formatting

- Formatting is enforced by `.editorconfig` via `dotnet format`. Run `dotnet format` before committing.
- Braces: always required, even when optional. Allman style (new line before opening brace).
- Max one statement per line, one assignment per statement.

### Preferred Patterns

- Primary constructors: `public class CacheService(IFileSystem fileSystem, ...)`.
- Pattern matching over `as`/`is` with null checks.
- Null coalescing (`??`), null propagation (`?.`), collection initializers.
- Expression-bodied accessors, indexers, lambdas, properties.
- Simple `using` statements (not `using` blocks).
- No `this.` qualification. Use language keywords for built-in types (`string`, not `String`).
- Prefer `readonly` fields, `readonly struct`, and `readonly struct member` where applicable.

## Testing Conventions

### Frameworks

- **xUnit** with `[Fact]` attributes (no `[Theory]` unless data-driven).
- **Moq** for mocking (`Mock<T>`, `It.IsAny<T>`, `.Verify()`).
- **xUnit Assert** for assertions (`Assert.Equal`, `Assert.ThrowsAsync`, etc.).
- **TestableIO.System.IO.Abstractions.TestingHelpers** for mock file systems.

### Test Structure

- **Class naming**: `<ClassName>Tests` (e.g., `CacheServiceTests`).
- **Method naming**: `MethodName_StateUnder_ExpectedBehavior` (e.g., `GetOrCreateDirectory_NonExistent_CreatesAndCallsFactory`).
- **Pattern**: Arrange-Act-Assert with explicit `// Arrange`, `// Act`, `// Assert` comments.
- **Constructor setup**: Initialize mocks and SUT in the test class constructor.
- **Async tests**: Use `public async Task`.
- **Namespaces**: File-scoped (e.g., `namespace Lip.Core.Tests.Services;`).
- **Mocks**: Declare as `private readonly Mock<IMyDependency> _mockDependency;`.

## Cross-Cutting Concerns

### Command Consistency

When modifying CLI commands, keep consistency across all three layers:
1. Command documentation in `docs/user-guide/commands`
2. Command implementation in `Lip.Cli`
3. Core services in `Lip.Core/Services`

### CI Pipeline

- **Build**: 6-platform matrix (linux-x64, linux-arm64, osx-x64, osx-arm64, win-x64, win-arm64).
- **Style check**: `dotnet format --verify-no-changes` on Windows.
- **Tests**: Run on all 6 platforms. Ensure tests pass cross-platform.

### Key Dependencies

- **Spectre.Console.Cli**: CLI command framework.
- **Flurl.Http**: HTTP client.
- **SharpCompress**: Archive handling.
- **CliWrap**: Process execution.
- **Semver**: Semantic versioning.
- **TestableIO System.IO.Abstractions**: File system abstraction (enables mock FS in tests).
- **StreamJsonRpc**: JSON-RPC for daemon communication.

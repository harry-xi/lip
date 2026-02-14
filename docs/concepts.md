# Core Concepts

This document explains the core concepts and data structures used in lip. Understanding these will help you use lip more effectively and understand how it manages packages.

## Package Identifiers

lip uses several ways to identify and specify packages.

### Package Identifier
A unique identifier for a package, typically coinciding with its repository path (e.g., `github.com/user/repo`). It is the most fundamental way to refer to a package without specifying a version.

### Package Specification
Combines a Package Identifier with a specific version requirement. It is used when you need to pin a exact version of a package.
- **Components**: Identifier, Version (SemVer).

### Local Package
Used when installing a package from a local directory or file. This is useful for development or installing packages that aren't hosted remotely.
- **Components**: Path (to the local `tooth.json` or directory).

### Remote Package
Used for packages specified by a direct URL, rather than a repository identifier. This allows installing packages from arbitrary locations.
- **Components**: URL.

## Manifest Structure (`tooth.json`)

The `tooth.json` file is the heart of a lip package. It describes the package metadata, dependencies, and installation rules.

### Manifest File
The root object of the `tooth.json` file.
- **Tooth**: The unique identifier (Package Identifier) of the package.
- **Version**: The semantic version of the package.
- **Description**: A brief description of what the package does.
- **Homepage**: URL to the project's homepage.
- **License**: The license under which the package is distributed.
- **Variants**: A list of Platform Variants defining platform-specific configurations.

### Platform Variant
Defines how the package should be installed on a specific platform or architecture.
- **Platform**: Specifies the target platform (e.g., `win-x64`, `linux-arm64`).
- **Assets**: A list of Assets.
- **Dependencies**: A map of required packages and their version ranges.
- **Scripts**: Lifecycle scripts (pre-install, post-install, etc.).
- **PreserveFiles**: List of glob patterns for files to keep during updates.
- **RemoveFiles**: List of glob patterns for files to remove.

### Asset
Describes a downloadable artifact associated with a variant.
- **Url**: The URL to download the asset from.
- **Hash**: Changes verification hash (SHA256).
- **Kind**: The type of asset (e.g., `Archive`, `Binary`, `Source`).

## Workspace & State

lip manages the state of installed packages within a workspace.

### Workspace State
Represents the current state of the workspace, tracking which packages are installed and how.
- **Packages**: A collection of installed packages.
- **Manifest**: The Manifest File of the current project (if applicable).

### Configuration
Manages global configuration settings for the lip tool itself, such as proxies and default behaviors.
- **Properties**: Key-value pairs for configuration options (e.g., `github_proxy`, `go_module_proxy`).

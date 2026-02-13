# Core Concepts

This document explains the core concepts and data structures used in lip, corresponding to the entities defined in `Lip.Core.Entities`. Understanding these will help you use lip more effectively and understand how it manages packages.

## Package Identifiers

lip uses several types to identify and specify packages.

### PackageId
`PackageId` represents the unique identifier of a package, typically coinciding with its repository path (e.g., `github.com/user/repo`). It is the most fundamental way to refer to a package without specifying a version.

### PackageSpec
`PackageSpec` combines a `PackageId` with a specific version requirement. It is used when you need to pinpoint a exact version of a package.
- **Components**: `PackageId`, `Version` (SemVer).

### LocalPackageSpec
`LocalPackageSpec` is used when installing a package from a local directory or file. This is useful for development or installing packages that aren't hosted remotely.
- **Components**: `Path` (to the local `tooth.json` or directory).

### RemotePackageSpec
`RemotePackageSpec` is used for packages specified by a direct URL, rather than a repository identifier. This allows installing packages from arbitrary locations.
- **Components**: `Url`.

## Manifest Structure (`tooth.json`)

The `tooth.json` file is the heart of a lip package. It describes the package metadata, dependencies, and installation rules.

### PackageManifest
The root object of the `tooth.json` file.
- **Tooth**: The unique identifier (PackageId) of the package.
- **Version**: The semantic version of the package.
- **Description**: A brief description of what the package does.
- **Homepage**: URL to the project's homepage.
- **License**: The license under which the package is distributed.
- **Variants**: A list of `PackageManifestVariant` objects defining platform-specific configurations.

### PackageManifestVariant
Defines how the package should be installed on a specific platform or architecture.
- **Platform**: Specifies the target platform (e.g., `win-x64`, `linux-arm64`).
- **Assets**: A list of `PackageManifestAsset` objects.
- **Dependencies**: A map of required packages and their version ranges.
- **Scripts**: Lifecycle scripts (pre-install, post-install, etc.).
- **PreserveFiles**: List of glob patterns for files to keep during updates.
- **RemoveFiles**: List of glob patterns for files to remove.

### PackageManifestAsset
Describes a downloadable artifact associated with a variant.
- **Url**: The URL to download the asset from.
- **Hash**: Changes verification hash (SHA256).
- **Kind**: The type of asset (e.g., `Archive`, `Binary`, `Source`).

## Workspace & State

lip manages the state of installed packages within a workspace.

### WorkspaceState
Represents the current state of the workspace, tracking which packages are installed and how.
- **Packages**: A collection of `WorkspaceStatePackage` objects representing installed packages.
- **Manifest**: The `PackageManifest` of the current project (if applicable).

### RuntimeConfig
Manages global configuration settings for the lip tool itself, such as proxies and default behaviors.
- **Properties**: Key-value pairs for configuration options (e.g., `github_proxy`, `go_module_proxy`).

# tooth.json

The `tooth.json` file is used to describe a package, including package metadata, identifier, version, dependencies, files, and other information. This file should be written in JSON format and placed in the root directory of the package.

For a JSON schema of `tooth.json`, see [tooth.v3.schema.json](../schemas/tooth.v3.schema.json).

Below, the (required/optional) annotation is used to indicate whether a field is required or optional of its upper level. For example, if `variants` is optional but `variants[].platform` is required, then you can omit `variants` field in `tooth.json`, but if there is any item in `variants`, you must provide `platform` field for each item.

## Fields

### format_version (required)

The version of the format. Currently, the only supported version is `3`.

### format_uuid (required)

The UUID of the format, used to identify the format. Currently, the only supported UUID is `289f771f-2c9a-4d73-9f3f-8492495a924d`.

### tooth (required)

The unique identifier of the package in the form of Go module path, i.e. a URL without scheme and any suffix. Optionally, you can specify a sub-directory path.

Examples:

- `github.com/LiteLDev/LeviLamina`
- `github.com/futrime/example-package#cmd/example-package`
- `github.com/LiteLDev/LegacyScriptEngine#quickjs`

The main part must be identical to the repository URL where the package is hosted if you want to publish the package.

### version (required)

The version of the package. Must be a valid [semantic version](https://semver.org).

Examples:

- `1.0.0`
- `1.0.0-alpha.1`
- `0.1.0`

When tagging a release, you should use the `v` prefix, e.g. `v1.0.0`. However, do not write the `v` prefix in the `version` field. Since Go module proxy will regard versions starting with `v0.0.0` as psuedo-versions, do not use `v0.0.0` as the version number.

### info (optional)

The metadata of the package.

### info.name (optional)

The name of the package.

### info.description (optional)

The description of the package.

### info.author (optional)

The author of the package.

### info.tags (optional)    

The tags of the package. The tags can be in two formats:

- `tag`: A single tag.
- `tag:subtag`: A key-value pair tag.

For `tag` and `subtag`, only lowercase letters, digits, and dashes are allowed ([a-z0-9-]). Though lip treats these two formats equally, some third-party tools and platforms may use them differently. For example, [Bedrinth](https://bedrinth.com) uses `tag:subtag` format to store some additional information about the package.

### info.avatar_url (optional)

The URL of the avatar of the package.

### variants (optional)

Array of objects defining different package variants for different platforms. This is the most important field in `tooth.json`.

When handling a package, lip will iterate over all the variants and process all variants matching the current platform in the order they are defined in `tooth.json`.

If more than one variant matches the current platform, lip will combine the fields of all matching variants.

However, on checking the compatibility of the package, lip will not consider the variants using glob patterns, i.e. if you want to support a wide range of platforms, you must define multiple variants, even if they are empty.

### variants[].platform (required)

The platform of the package variant. Can be one of the following:

- `linux-arm64`
- `linux-x64`
- `osx-arm64`
- `osx-x64`
- `win-arm64`
- `win-x64`
- A glob pattern, e.g. `linux-*`

### variants[].dependencies (optional)

The dependencies of the package variant. The keys are the identifiers of the dependencies, which may contain a sub-directory path. The values are the versions or version ranges of the dependencies.

Key examples:

- `github.com/futrime/example-package`
- `github.com/futrime/example-package#subpath`

Value examples:

- `1.0.0`
- `1.0.0-alpha.1`
- `0.1.0`
- `>=0.1.0`
- `>=0.1.0 <1.0.0`

We use [WalkerCodeRanger/semver](https://github.com/WalkerCodeRanger/semver) to parse the versions and version ranges.

### variants[].prerequisites (optional)

The prerequisites of the package variant. The format is the same as `variants[].dependencies`. Different from `variants[].dependencies`, the prerequisites will not be installed automatically. If a prerequisite is not installed, lip will refuse to install the package.

### variants[].assets (optional)

Describe how the files in the package should be handled.

### variants[].assets[].type (required)

The type of the asset. Can be one of the following:

- `self`: The asset is the package itself, i.e. the files in the package directory or the package archive.
- `uncompressed`: The asset is a single uncompressed file.
- `tar`: The asset is a tar file.
- `tar.gz`: The asset is a tar.gz file.
- `zip`: The asset is a zip file.

### variants[].assets[].urls (required)

The URLs to fetch the asset from. lip will attempt to download the asset from the URLs in the order they are defined here. For `self` asset type, the URLs should be empty, otherwise lip will refuse to install the package.

### variants[].assets[].place (optional)

An array to specify how files in the tooth should be place to the workspace.

### variants[].assets[].place[].type (required)

The type of the place. Can be one of the following:

- `file`: The `src` and `dest` are both files.
- `dir`: The `src` and `dest` are both directories.

For `uncompressed` asset type, only `file` type is allowed.

### variants[].assets[].place[].src (required)

The source path of the file, or a directory, or a glob pattern for files. For `uncompressed` asset type, the downloaded file will be the only file that can be placed, and the corresponding `src` filed should be empty (`""`). For `file` type, `src` should be a file or a glob pattern for files, and all matched directories will be ignored. The matched files will be flattened. For example, if `src` matches `foo/bar/baz.txt` and `foo/kt.txt`, their directory structure will be flattened to `baz.txt` and `kt.txt` before being placed. For `directory` type, `src` should be a directory, and all files in the directory will be placed, keeping the directory structure.

### variants[].assets[].place[].dest (required)

The destination path of the file. If `src` is a file, `dest` should be a file. If `src` is a directory or a file glob pattern, `dest` should be a directory.

### variants[].assets[].preserve (optional)

The path or glob pattern to preserve in the workspace. The files will not be removed when the package is uninstalled. Items in this array should not exist in `remove` array.

### variants[].assets[].remove (optional)

The path or glob pattern to remove in the workspace. The files will be removed when the package is uninstalled. Items in this array should not exist in `preserve` array.

### variants[].scripts (optional)

The commands to run in the workspace. The keys are the names of the scripts, and the values are the commands to run.

Here is a list of predefined scripts that will be run at corresponding stages:

- `pre_install`: Before installing the package.
- `install`: After placing the files.
- `post_install`: After installing the package.
- `pre_pack`: Before packing the package.
- `post_pack`: After packing the package.
- `pre_uninstall`: Before uninstalling the package.
- `uninstall`: After removing the files.
- `post_uninstall`: After uninstalling the package.

For other scripts, you can define your own scripts in the `scripts` field and run with `lip run <script>`.

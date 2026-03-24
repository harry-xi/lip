# MEMORY.md

## Memo

- `lip.sln` exists at the repo root and currently includes the main code projects under `src/` plus test projects under `tests/`; `Lip.Installer` is referenced by the release workflow but its project files may not always be present in the working tree.
- `Lip.Cli/Program.cs` is a thin Spectre.Console front end; most operational wiring happens in `LipClient.Create`, which constructs `ConfigService`, `CacheService`, `SourceService`, `WorkspaceService`, `PackageInstaller`, `CompositePackageRegistry`, and `InstallService`.
- Workspace state is persisted in `tooth_lock.json` in the current working directory; it stores installed package manifests, variant labels, explicit/implicit status, and tracked file paths.
- Global runtime config is persisted in `liprc.json` under `Environment.SpecialFolder.ApplicationData` in `lip/liprc.json`; cache and temp data live under `Environment.SpecialFolder.LocalApplicationData` in `lip/cache` and `lip/temp`.
- `tooth.json` is the package manifest and `lip init` creates a minimal one, but current install/uninstall flows do not mutate `tooth.json`; they operate on workspace files plus `tooth_lock.json`.
- Durable docs caveat: `docs/faq.md` matches the current implementation on `tooth.json`, but `docs/intro/quick_start.md` still says install/uninstall update the manifest; trust the code and FAQ over that quick-start wording.
- Manifest, runtime config, and workspace state all use format version `3` with UUID `289f771f-2c9a-4d73-9f3f-8492495a924d`.
- Package argument parsing in `LipClient` tries, in order: exact `PackageSpec`, flexible `PackageId`, local archive path, then remote archive URL.
- Package source retrieval prefers Go module proxy first and Git second; download URLs may be rewritten through `github_proxy`, and `go_module_proxy` defaults to `https://goproxy.io`.
- Git fallback currently caches cloned repositories as directories via `CacheService.GetOrCreateDirectory`; this is fragile because `.git` contents can make `cache clean` deletion fail and directory-structured cache entries are riskier than file-archive cache artifacts.
- Package registry lookup is layered: installed workspace packages first, then remote registries (`GoModuleProxyPackageRegistry`, `LiprPackageRegistry`, `GitPackageRegistry`), then source-derived manifests.
- With dependencies enabled, install/uninstall/update recompute the full desired dependency graph from explicitly installed packages, uninstall removed packages in reverse topological order, then install new ones in topological order.
- `--no-dependencies` bypasses dependency solving and is explicitly treated as potentially leaving a broken workspace.
- Package variants are merged by label and current RID platform match; platform matching supports glob patterns and requires at least one full label+platform match.
- Package install order is: run pre-install scripts, place asset files, run install/post-install scripts, then record workspace state. Uninstall runs pre-uninstall/uninstall scripts, removes tracked files except `preserve_files`, applies `remove_files`, runs post-uninstall scripts, then removes state.
- CI expectations for code changes are `dotnet format --verify-no-changes lip.sln` and `dotnet test lip.sln`; docs are a separate VitePress project under `docs/` built with `npm run build`.
- Native npm distribution is driven from `npm/`; the single `@futrime/lip` package exposes root launchers `lip.js` and `lipd.js`, bundles both `lip` and `lipd` for all six supported platforms in root-level platform folders like `linux-x64/` and `win32-x64/`, and the release workflow rewrites the package version from the release event tag before publishing.
- GitHub release automation now uploads GitHub Release archives directly inside each `build` matrix run after artifact upload; `publish-npm` still downloads all six `build` artifacts, copies their binaries into the staged `@futrime/lip` package, and publishes one npm package version derived from `github.event.release.tag_name` with a shell-side `v` prefix trim.
- For the single-package npm distribution, `dotnet publish` must include both `-r <runtime>` and `--no-self-contained`; this keeps `PublishSingleFile=true` outputs framework-dependent and small enough for npm publish. Without `--no-self-contained`, each binary grows to roughly 75-90 MB and the combined npm tarball becomes too large for npm CLI publish.
- npm CLI 11 rejects prerelease publishes unless `npm publish` includes an explicit `--tag`; for versions like `0.34.2-fd.1`, release CI should publish with dist-tag `fd` rather than relying on npm defaults.
- Windows installer packaging no longer uses the removed `src/Lip.Installer` WiX project; release CI now builds `lip-<version>-<runtime>-setup.exe` from `inno/lip.iss`, which expects downloaded binaries under `.tmp/artifacts` and writes installers to `.tmp/nsis`.
- `inno/lip.iss` must not branch on an undeclared `Runtime` preprocessor symbol; release CI should pass the resolved Windows installer architecture explicitly (for example `BuildArch=arm64` or `x64compatible`) into Inno Setup.

## Recent

- 2026-03-24: Fixed release CI for prerelease tags by publishing npm prereleases with an explicit dist-tag derived from the SemVer prerelease label (for example `0.34.2-fd.1` -> `--tag fd`) and by passing Inno Setup `BuildArch` from the workflow instead of branching on an undeclared `Runtime` symbol inside `inno/lip.iss`.
- 2026-03-24: Windows release packaging in `.github/workflows/release.yml` now uses the `create-windows-installer` matrix job plus a minimal `inno/lip.iss` with fixed relative paths `.tmp/artifacts` and `.tmp/nsis`, auto-adds the install directory to system PATH, and uploads `lip-<version>-<runtime>-setup.exe` installers for both `win-x64` and `win-arm64`.
- 2026-03-24: `.github/workflows/release.yml` now derives all release job versions from `github.event.release.tag_name` via `echo "VERSION=$(echo '${{ github.event.release.tag_name }}' | sed 's/^v//')" >> "$GITHUB_OUTPUT"` and removed extra `actions/checkout` fetch options that were only needed for `git describe --tags`.
- 2026-03-24: `lip.sln` now exists at the repo root, so standard verification should target `lip.sln` instead of individual `.csproj` paths.
- 2026-03-23: Published `@futrime/lip@0.34.2-fd.0` to npm with tag `fd` after rebuilding all six platform binaries using `dotnet publish -r <runtime> --no-self-contained`; npm currently also reports `latest` as `0.34.2-fd.0`.
- 2026-03-23: Flattened npm package native payloads from `bin/<platform>` to root-level platform folders like `linux-x64/`; updated launchers, npm `files`, and release CI copy paths accordingly.
- 2026-03-23: Moved npm launchers to `npm/lip.js` and `npm/lipd.js`, and updated release CI so the single `@futrime/lip` package now bundles both `lip` and `lipd` binaries for every supported platform.
- 2026-03-23: Merged the `upload-assets` release workflow job into the `build` matrix so each platform build now uploads its own GitHub Release archive directly, and `publish-winget` depends on `build` instead of a separate asset-upload phase.
- 2026-03-23: Simplified npm distribution to a single `@futrime/lip` package that bundles all six platform binaries; removed platform subpackage manifests and changed release CI to stage one package from all build artifacts.
- 2026-03-23: Cleaned `MEMORY.md` npm notes to keep only durable workflow facts and avoid transient command-line setup details.

## History

- 2026-03-23: Minimized npm release CI in `.github/workflows/release.yml` by deriving npm versions from the release tag and publishing platform packages directly from `build` artifacts instead of downloading GitHub Release archives.
- 2026-03-23: Refactored `.github/workflows/release.yml` so the npm publish jobs use separate GitHub Actions steps for package staging, asset download, package version rewriting, binary extraction, and `npm publish`.
- 2026-03-23: Simplified npm release CI by deleting `npm/scripts/stage-pnpm-release.sh` and `npm/scripts/publish-pnpm-release.sh`; `.github/workflows/release.yml` now uses a matrix job for platform packages plus a follow-up job for `@futrime/lip`.
- 2026-03-23: Wired npm publishing into `.github/workflows/release.yml` as a `publish-npm` job that runs after release assets are uploaded and uses the `NPM_TOKEN` secret for npm auth.
- 2026-03-23: Removed the temporary root `package.json` and `pnpm-workspace.yaml`; npm publishing is now driven directly by the scripts under `npm/scripts/`.
- 2026-03-23: Moved the npm publish helper scripts under `npm/scripts/` and confirmed staging still works from the new location.
- 2026-03-23: The original `@futrime/lip@0.34.2` npm record was later unpublished, which blocks re-publishing the same version; the framework-dependent replacement was published as `0.34.2-fd.0` instead.
- 2026-03-23: Added a `pnpm`-based native npm distribution layout for `@futrime/lip` plus six platform packages, staging binaries from GitHub Release assets instead of local builds; verified all seven packages pass `pnpm publish --dry-run` for `v0.34.2`, with actual publish currently blocked only by missing npm auth.
- 2026-03-23: Initialized repo memory from code/docs/CI and recorded the Git source fallback caveat about directory-based cache entries making `cache clean` brittle on `.git` contents.
- 2026-03-23: Merged two conflicting `AGENTS.md` templates into one repo-specific policy file, removed Python/Notion workflow rules, and aligned instructions with the repo's `.NET` and docs build workflows.
- 2026-03-23: Fixed `MEMORY.md` structure to match policy, with `Memo`, `Recent`, `History`, and normalized `Memo` casing.

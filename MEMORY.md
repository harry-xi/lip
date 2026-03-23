# MEMORY.md

## Memo

- `lip.sln` contains five main code projects: `Lip.Core` (domain/services), `Lip.Cli` (user CLI), `Lip.Daemon` (`lipd` JSON-RPC daemon over stdio), `Golang.Org.X.Mod` (Go module path/version helpers), and `Lip.Installer` (WiX installer).
- `Lip.Cli/Program.cs` is a thin Spectre.Console front end; most operational wiring happens in `LipClient.Create`, which constructs `ConfigService`, `CacheService`, `SourceService`, `WorkspaceService`, `PackageInstaller`, `CompositePackageRegistry`, and `InstallService`.
- Workspace state is persisted in `tooth_lock.json` in the current working directory; it stores installed package manifests, variant labels, explicit/implicit status, and tracked file paths.
- Global runtime config is persisted in `liprc.json` under `Environment.SpecialFolder.ApplicationData` in `lip/liprc.json`; cache and temp data live under `Environment.SpecialFolder.LocalApplicationData` in `lip/cache` and `lip/temp`.
- `tooth.json` is the package manifest and `lip init` creates a minimal one, but current install/uninstall flows do not mutate `tooth.json`; they operate on workspace files plus `tooth_lock.json`.
- Durable docs caveat: `docs/faq.md` matches the current implementation on `tooth.json`, but `docs/intro/quick_start.md` still says install/uninstall update the manifest; trust the code and FAQ over that quick-start wording.
- Manifest, runtime config, and workspace state all use format version `3` with UUID `289f771f-2c9a-4d73-9f3f-8492495a924d`.
- Package argument parsing in `LipClient` tries, in order: exact `PackageSpec`, flexible `PackageId`, local archive path, then remote archive URL.
- Package source retrieval prefers Go module proxy first and Git second; download URLs may be rewritten through `github_proxy`, and `go_module_proxy` defaults to `https://goproxy.io`.
- Package registry lookup is layered: installed workspace packages first, then remote registries (`GoModuleProxyPackageRegistry`, `LiprPackageRegistry`, `GitPackageRegistry`), then source-derived manifests.
- With dependencies enabled, install/uninstall/update recompute the full desired dependency graph from explicitly installed packages, uninstall removed packages in reverse topological order, then install new ones in topological order.
- `--no-dependencies` bypasses dependency solving and is explicitly treated as potentially leaving a broken workspace.
- Package variants are merged by label and current RID platform match; platform matching supports glob patterns and requires at least one full label+platform match.
- Package install order is: run pre-install scripts, place asset files, run install/post-install scripts, then record workspace state. Uninstall runs pre-uninstall/uninstall scripts, removes tracked files except `preserve_files`, applies `remove_files`, runs post-uninstall scripts, then removes state.
- CI expectations for code changes are `dotnet format --verify-no-changes` and `dotnet test`; docs are a separate VitePress project under `docs/` built with `npm run build`.

## Recent

- 2026-03-23: Initialized `MEMORY.md` `Memo` by reading the repo structure, core services, docs, schemas, and CI; recorded stable architecture, state/config locations, dependency-resolution behavior, and a `tooth.json` docs drift caveat.
- 2026-03-23: Merged two conflicting `AGENTS.md` templates into one repo-specific policy file, removed Python/Notion workflow rules, and aligned instructions with the repo's `.NET` and docs build workflows.
- 2026-03-23: Fixed `MEMORY.md` structure to match policy: `Memo`, `Recent`, then `History`, and normalized `Memo` casing.

## History

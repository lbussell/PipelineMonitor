# Development

## Getting Started

### Prerequisites

- Install the [.NET SDK](https://dotnet.microsoft.com/download)

### Build and Test

Build the project:

```bash
dotnet build
```

Run tests:

```bash
dotnet test
```

## Publishing Your Package

This repository includes GitHub Actions workflows for automated building and
publishing to NuGet.org.

### Setting Up Trusted Publishing

Run the setup script to configure GitHub and NuGet.org for trusted publishing:

```bash
dotnet run scripts/SetupPublishing.cs
```

### How Versioning Works

This project uses [MinVer](https://github.com/adamralph/minver) for automatic
versioning from git tags. Versions follow [Semantic
Versioning](https://semver.org/).

- **Tagged commits** get the exact version from the tag (e.g. tag `v1.0.0` →
  version `1.0.0`)
- **Untagged commits** automatically get a development pre-release version.
  MinVer increments the patch number and appends `alpha.0.{height}`, where
  height is the number of commits since the last tag. For example, 5 commits
  after `v1.0.0` produces version `1.0.1-alpha.0.5`. These versions are for
  local development only — they are never published to NuGet.

When you're ready to release a pre-release version intentionally (e.g.
`1.0.0-rc.1`, `1.0.0-preview.1`), use the release script to create a tag with
your chosen label. This is distinct from the automatic `alpha.0` versions that
MinVer generates between tags.

No manual version editing is needed in `.csproj` files.

### Publishing a Stable Release

Use the release script:

```bash
dotnet run scripts/Release.cs
```

The script lists existing tags, prompts for the version (e.g. `1.0.0`), creates
and pushes the tag, and creates a GitHub Release with auto-generated notes. The
**Publish NuGet Package** workflow triggers automatically.

After the release, remember to update the version in
`.claude-plugin/plugin.json` and `.claude-plugin/marketplace.json`.

### Publishing a Pre-Release

Use the same release script and enter a pre-release version when prompted (e.g.
`1.0.0-rc.1`, `0.6.0-alpha.1`, `0.6.0-preview.1`). The script automatically
detects the pre-release suffix and marks the GitHub Release as a pre-release.

```bash
dotnet run scripts/Release.cs
# When prompted, enter: 1.0.0-rc.1
```

The **Publish NuGet Package** workflow triggers automatically when the tag is
pushed.

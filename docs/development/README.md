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
- **Untagged commits** automatically get a pre-release version with height
  (e.g. `1.0.1-alpha.0.5` means 5 commits after `v1.0.0`)

No manual version editing is needed in `.csproj` files.

### Publishing a Stable Release

Use the release script to create and push a release tag:

```bash
dotnet run scripts/Release.cs
```

The script:

1. Lists existing tags for reference
2. Prompts for the version to release
3. Creates a `v{version}` git tag on the current commit
4. Pushes the tag to origin
5. The **Publish NuGet Package** workflow triggers automatically

After the release, remember to update the version in
`.claude-plugin/plugin.json` and `.claude-plugin/marketplace.json`.

### Publishing a Pre-Release

Tag the commit with a pre-release version:

```bash
git tag v1.0.0-rc.1
git push --tags
```

The **Publish NuGet Package** workflow triggers automatically when the tag is
pushed.

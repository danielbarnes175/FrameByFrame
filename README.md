# Frame by Frame

![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/v/release/danielbarnes175/FrameByFrame?include_prereleases) ![GitHub All Releases](https://img.shields.io/github/downloads/danielbarnes175/FrameByFrame/total) ![GitHub repo size](https://img.shields.io/github/repo-size/danielbarnes175/FrameByFrame) 

Frame by Frame is a simple animation program for creating frame by frame animations.

<img width="300" alt="image" src="https://github.com/user-attachments/assets/bd0825c1-f058-4353-8182-4e947430afd0" />
<img width="300" alt="image" src="https://github.com/user-attachments/assets/c10c4d92-a1e4-4451-9275-a29e7435b2ba" />
<img width="300" alt="image" src="https://github.com/user-attachments/assets/346b486e-4317-47db-8a6c-fe768feb2236" />

## Features
- Onion skinning for easy frame-by-frame animation
- Multiple layers support
- Basic drawing tools: brush, eraser, color picker
- Simple timeline navigation (next/previous/first/last frame)
- Export animations to GIF
- Customizable brush sizes
- Intuitive and minimal UI

## Setup
Prerequisites: `dotnet-sdk-8.0`

**Download:**
Please see the latest [release](https://github.com/danielbarnes175/FrameByFrame/releases)

**Build from source:**
- `cd FrameByFrame` (directory w/ `FrameByFrame.csproj`)
- `dotnet build`
- `dotnet run`

## Publish a new version

Publishing requires the .NET 8 SDK, `git`, `zip`, and an authenticated [GitHub CLI](https://cli.github.com/). Commit or stash all local changes before starting because the release script requires a clean working tree.

To build the release archive for Linux x64, Windows x64, Intel macOS, and Apple Silicon macOS without creating a tag or GitHub release, pass the version to the build script:

```bash
./scripts/release.sh v0.1.0
```

This creates `release/FrameByFrame-v0.1.0.zip` containing all four platform builds.

From the repository root, pass the new version number to the publish script:

```bash
./scripts/publish-release.sh v0.1.0
```

The version may be provided with or without the `v` prefix. The script builds the supported platform versions, creates `release/FrameByFrame-v0.1.0.zip`, creates and pushes the `v0.1.0` tag, and publishes a GitHub release with the ZIP attached and generated release notes.

## Contributing

To contribute to Frame by Frame, please view our [Contributing Guidelines](CONTRIBUTING.md) and our [Code of Conduct](CODE_OF_CONDUCT.md).

## Contact

For questions or concerns, feel free to file an issue.

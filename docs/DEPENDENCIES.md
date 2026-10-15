# Dependency maintenance

FrameByFrame pins managed packages, MonoGame tools, and bundled FFmpeg binaries to reviewed versions.

## Managed packages and tools

Dependabot checks NuGet packages and GitHub Actions weekly. Update MonoGame framework, content builder, and MGCB tools together. Validate updates with a clean restore, Debug and Release builds, tests, and content compilation.

## Bundled FFmpeg

Release builds download FFmpeg from the `eugeneware/ffmpeg-static` GitHub releases. The project currently uses release `b6.1.1`, the latest compatible upstream release verified on October 15, 2026.

Every supported runtime identifier has a pinned SHA-256 hash for the executable and license. Never update the version without updating and independently verifying those hashes. The build must fail when any downloaded file does not match its expected hash.

Review FFmpeg releases and security advisories at least monthly. Test audio import plus MOV and MP4 export before publishing an updated binary.

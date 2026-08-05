# FrameByFrame Project File Format

## Status

This document defines FrameByFrame project format version `1.0`.

- File extension: `.fbf`
- MIME type: not yet registered
- Byte order: little-endian
- Character encoding: UTF-8
- Compression: Brotli
- Current writer keyframe interval: 100 frames

An FBF file is the editable source project. PNG sequences and GIF files are derived exports and are not part of this format.

## Goals

The format is designed to:

- preserve all editable animation layers;
- scale with the amount of artwork that changes rather than canvas area alone;
- represent repeated frames with minimal overhead;
- allow bounded, frame-at-a-time compression and decompression;
- provide an index for future lazy and random-access loading;
- permit format evolution through explicit version and feature fields;
- reject corrupt or unreasonably large inputs before allocating major resources.

## Non-goals for version 1

Version 1 does not provide:

- encryption;
- checksums or cryptographic authentication;
- different canvas dimensions per frame;
- per-frame timing;
- vector drawing data;
- embedded exported images;
- backward compatibility with the pre-FBF JSON prototype.

## Primitive types

| Name | Size | Encoding |
| --- | ---: | --- |
| `u8` | 1 byte | Unsigned integer |
| `u16` | 2 bytes | Unsigned little-endian integer |
| `u32` | 4 bytes | Unsigned little-endian integer |
| `i32` | 4 bytes | Signed little-endian integer |
| `i64` | 8 bytes | Signed little-endian integer |
| `f32` | 4 bytes | IEEE 754 single-precision, little-endian |
| `varuint` | 1–5 bytes | Unsigned LEB128-style integer |
| `string` | variable | `i32` byte length followed by UTF-8 bytes |

All offsets are absolute byte offsets from the beginning of the file.

## Overall layout

```text
+------------------------------+
| File header                  |
+------------------------------+
| Frame chunk 0                |
+------------------------------+
| Frame chunk 1                |
+------------------------------+
| ...                          |
+------------------------------+
| Frame chunk N-1              |
+------------------------------+
| Frame index                  |
+------------------------------+
| Footer                       |
+------------------------------+
```

Frame chunks occur in ascending frame order. The index also records each chunk's absolute offset.

## File header

| Field | Type | Required value or meaning |
| --- | --- | --- |
| Magic | 8 bytes | ASCII `FBFPROJ` followed by `0x00` |
| Major version | `u16` | `1` |
| Minor version | `u16` | `0` |
| Feature flags | `u32` | Bit 0 set for Brotli frame compression; all other bits zero |
| Canvas width | `i32` | Pixels, greater than zero |
| Canvas height | `i32` | Pixels, greater than zero |
| Canvas X | `f32` | Drawing canvas screen position |
| Canvas Y | `f32` | Drawing canvas screen position |
| FPS | `i32` | Playback frames per second, greater than zero |
| Layer count | `i32` | Number of project layers, from `1` through `1024` |
| Frame count | `i32` | Greater than zero |
| Keyframe interval | `i32` | Greater than zero; currently `100` |
| Project name | `string` | Non-empty UTF-8 project name |
| Layer metadata | repeated | One metadata record per layer, in front-to-back order |
| Index offset | `i64` | Absolute offset of the `INDX` signature |

Each layer metadata record contains a 16-byte GUID, a non-empty name string, a visibility boolean, and a lock boolean. The GUID is the stable identity used to associate each frame's pixel section with its project-wide layer definition.

The writer initially reserves the index-offset field, writes all frame chunks and the index, then seeks back and fills in the final offset.

## Pixel representation

A canvas pixel is addressed by a zero-based linear index:

```text
index = y * canvasWidth + x
x = index % canvasWidth
y = index / canvasWidth
```

Pixel indexes are stored as `varuint` values.

Colors use MonoGame's 32-bit `Color.PackedValue`. A packed value of zero is transparent. Within delta frames, zero means remove that pixel from the layer.

Only non-transparent pixels exist in a reconstructed layer map.

## Frame chunk

Each frame starts with an uncompressed chunk header:

| Field | Type | Meaning |
| --- | --- | --- |
| Magic | 4 bytes | ASCII `FRAM` |
| Frame index | `i32` | Zero-based index; must match its index-table position |
| Frame kind | `u8` | `0` keyframe, `1` delta, `2` same as previous |
| Uncompressed length | `i32` | Decompressed payload length |
| Compressed length | `i32` | Brotli payload length |
| Payload | byte array | `Compressed length` bytes |

### Frame kind 0: keyframe

A keyframe resets all reconstructed layers before applying its records. Its records contain every non-transparent pixel in the frame.

Frame zero must be a keyframe. The current writer also emits a keyframe every 100 frames. Periodic keyframes bound the number of deltas required for future random-access decoding and limit corruption propagation.

### Frame kind 1: delta

A delta starts from the preceding reconstructed frame. It stores only:

- pixels newly added;
- pixels whose packed color changed;
- pixels removed from the layer.

A removal is represented by the affected pixel index and a packed color of zero.

### Frame kind 2: same as previous

This record means all layers are identical to the preceding frame.

For this frame kind:

- frame index must be greater than zero;
- uncompressed length must be zero;
- compressed length must be zero;
- no payload follows.

## Decompressed frame payload

Keyframe and delta payloads contain one section per layer in the same front-to-back order as the header metadata.

Each layer section is:

| Field | Type | Meaning |
| --- | --- | --- |
| Change count | `i32` | Number of pixel records in this layer |
| Pixel records | repeated | Exactly `Change count` records |

Each pixel record is:

| Field | Type | Meaning |
| --- | --- | --- |
| Pixel index | `varuint` | Linear canvas pixel index |
| Packed color | `u32` | MonoGame packed color; zero removes a delta pixel |

The entire layer payload is compressed as one Brotli stream using the optimal compression level. Compression is scoped to one frame, so the writer and reader never need to buffer the complete project file.

## Variable-length pixel indexes

Pixel indexes use seven payload bits per byte. Bit 7 indicates that another byte follows.

Example encodings:

| Value | Bytes |
| ---: | --- |
| `0` | `00` |
| `127` | `7F` |
| `128` | `80 01` |
| `16384` | `80 80 01` |

Readers reject encodings longer than five bytes.

## Frame index

The index begins at the absolute offset recorded in the header.

| Field | Type | Meaning |
| --- | --- | --- |
| Magic | 4 bytes | ASCII `INDX` |
| Entry count | `i32` | Must equal the header frame count |
| Frame offsets | `i64[]` | One absolute frame-chunk offset per frame |

Offsets must be strictly increasing, greater than zero, and less than the index offset.

Although the initial reader reconstructs every frame eagerly, this table permits a future reader to seek to the nearest preceding keyframe and apply only the required deltas.

## Footer

The footer immediately follows the frame index:

| Field | Type | Meaning |
| --- | --- | --- |
| Magic | 4 bytes | ASCII `FBFE` |
| Index offset | `i64` | Must match the header index offset |

The duplicate offset allows readers and repair utilities to locate or validate the index from either end of the file.

## Save algorithm

For each frame, the writer constructs sparse maps from pixel index to packed color for each layer.

1. Frame zero and every configured interval become keyframes.
2. If all layer maps equal the previous frame, write a same-as-previous chunk.
3. Otherwise, compare current and previous maps:
   - write additions and changed colors;
   - write removed indexes with color zero.
4. Brotli-compress that frame's payload.
5. Record the frame chunk offset.
6. After all frames, write the index and footer.
7. Patch the header index offset.
8. Flush the temporary file to disk and atomically replace the destination.

If saving fails, the temporary file is deleted and an existing project file remains unchanged.

## Load algorithm

1. Validate the signature, major version, flags, dimensions, FPS, layer count, frame count, and keyframe interval.
2. Read and validate the frame index and footer.
3. Starting with empty layer maps, visit frame chunks in index order.
4. For a keyframe, clear all maps before applying records.
5. For a delta, apply records to the previous maps.
6. For a same-as-previous frame, leave the maps unchanged.
7. Reconstruct an editable FrameByFrame frame from the resulting maps.

The application currently performs step 7 for every frame during project loading. A future implementation may keep compressed chunks on disk and materialize frames on demand.

## Reader validation requirements

A conforming reader should reject a file when any of the following is true:

- a required signature is incorrect;
- the major version is unsupported;
- unknown required feature flags are present;
- dimensions, FPS, layer count, frame count, or interval are invalid;
- integer arithmetic overflows;
- a string or frame payload exceeds implementation safety limits;
- index and footer offsets disagree;
- frame offsets are unordered or outside the frame area;
- a frame index differs from its index-table position;
- an unknown frame kind is encountered;
- compressed or uncompressed lengths are invalid;
- Brotli output does not exactly match its declared length;
- a layer change count is invalid;
- a pixel index lies outside the canvas;
- decompressed payload bytes remain after all declared layer sections.

## Versioning

Readers use the major version to determine structural compatibility.

- A different major version is incompatible and must be rejected.
- A greater minor version may only be accepted when its feature flags and added data are explicitly understood.
- New mandatory behavior requires a new feature flag or major version.
- New optional metadata should be introduced in a future extensible metadata section rather than silently changing existing records.

## Expected scaling

Storage depends primarily on changes between frames:

- unchanged frame: one 17-byte frame header and one 8-byte index entry;
- sparse edit: changed indexes and packed colors, then Brotli compression;
- periodic keyframe: all non-transparent pixels in that frame;
- completely different dense frames: necessarily large, because the underlying image information is large.

The format avoids per-file PNG overhead, repeated storage of unchanged frames, and verbose textual pixel objects.

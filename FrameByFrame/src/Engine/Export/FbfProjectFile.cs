using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using FrameByFrame.src.Engine.Animation;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.Export
{
    internal static class FbfProjectFile
    {
        private static readonly byte[] FileMagic = Encoding.ASCII.GetBytes("FBFPROJ\0");
        private static readonly byte[] FrameMagic = Encoding.ASCII.GetBytes("FRAM");
        private static readonly byte[] IndexMagic = Encoding.ASCII.GetBytes("INDX");
        private static readonly byte[] FooterMagic = Encoding.ASCII.GetBytes("FBFE");

        private const ushort MajorVersion = 1;
        private const ushort MinorVersion = 0;
        private const uint BrotliFlag = 1;
        private const int MaxLayerCount = 1024;
        private const int KeyframeInterval = 100;
        private const int MaxStringBytes = 1024 * 1024;
        private const int MaxFrameCount = 1_000_000;
        private const int MaxDimension = 4_096;
        private const long MaxDecodedPixelSlots = 268_435_456;
        private const int MaxFramePayloadBytes = 512 * 1024 * 1024;

        private enum FrameKind : byte
        {
            Keyframe = 0,
            Delta = 1,
            SameAsPrevious = 2
        }

        private readonly record struct PixelChange(int Index, uint PackedColor);

        public static void Save(string filename, Animation.Animation animation)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filename);
            ArgumentNullException.ThrowIfNull(animation);

            if (animation.frames.Count == 0)
                throw new InvalidDataException("Cannot save an animation without frames.");
            if (animation.fps <= 0)
                throw new InvalidDataException("Animation FPS must be greater than zero.");
            if (animation.Layers.Count == 0 || animation.Layers.Count > MaxLayerCount)
                throw new InvalidDataException("Animation layer count is invalid or unsupported.");

            Frame firstFrame = animation.frames.First.Value;
            ValidateDimensions(firstFrame.width, firstFrame.height);
            ValidateResourceBudget(firstFrame.width, firstFrame.height,
                animation.Layers.Count, animation.frames.Count);

            string temporaryFilename = filename + ".tmp";
            try
            {
                using (FileStream stream = File.Open(temporaryFilename, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                {
                    WriteBytes(writer, FileMagic);
                    writer.Write(MajorVersion);
                    writer.Write(MinorVersion);
                    writer.Write(BrotliFlag);
                    writer.Write(firstFrame.width);
                    writer.Write(firstFrame.height);
                    writer.Write(animation.framePosition.X);
                    writer.Write(animation.framePosition.Y);
                    writer.Write(animation.fps);
                    writer.Write(animation.Layers.Count);
                    writer.Write(animation.frames.Count);
                    writer.Write(KeyframeInterval);
                    WriteString(writer, animation.projectName);
                    foreach (AnimationLayer layer in animation.Layers)
                    {
                        writer.Write(layer.Id.ToByteArray());
                        WriteString(writer, layer.Name);
                        writer.Write(layer.IsVisible);
                        writer.Write(layer.IsLocked);
                    }

                    long indexOffsetPosition = stream.Position;
                    writer.Write((long)0);

                    var frameOffsets = new List<long>(animation.frames.Count);
                    Dictionary<int, uint>[] previousLayers = CreateEmptyLayers(animation.Layers.Count);
                    int frameIndex = 0;

                    foreach (Frame frame in animation.frames)
                    {
                        if (frame.width != firstFrame.width || frame.height != firstFrame.height)
                            throw new InvalidDataException("All frames in an FBF v1 project must have identical dimensions.");

                        Dictionary<int, uint>[] currentLayers = SnapshotLayers(frame, animation.Layers);
                        bool isKeyframe = frameIndex % KeyframeInterval == 0;
                        FrameKind frameKind = isKeyframe
                            ? FrameKind.Keyframe
                            : LayersEqual(previousLayers, currentLayers)
                                ? FrameKind.SameAsPrevious
                                : FrameKind.Delta;

                        frameOffsets.Add(stream.Position);
                        WriteFrameChunk(writer, frameIndex, frameKind, previousLayers, currentLayers);
                        previousLayers = currentLayers;
                        frameIndex++;
                    }

                    long indexOffset = stream.Position;
                    WriteBytes(writer, IndexMagic);
                    writer.Write(frameOffsets.Count);
                    foreach (long frameOffset in frameOffsets)
                        writer.Write(frameOffset);

                    WriteBytes(writer, FooterMagic);
                    writer.Write(indexOffset);

                    stream.Position = indexOffsetPosition;
                    writer.Write(indexOffset);
                    writer.Flush();
                    stream.Flush(flushToDisk: true);
                }

                File.Move(temporaryFilename, filename, overwrite: true);
            }
            catch
            {
                if (File.Exists(temporaryFilename))
                    File.Delete(temporaryFilename);
                throw;
            }
        }

        public static Animation.Animation Load(string filename)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filename);

            using FileStream stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            using BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            ExpectBytes(reader, FileMagic, "FBF file signature");
            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();
            if (majorVersion != MajorVersion || minorVersion > MinorVersion)
                throw new InvalidDataException($"Unsupported FBF version {majorVersion}.{minorVersion}.");

            uint flags = reader.ReadUInt32();
            if ((flags & BrotliFlag) == 0 || (flags & ~BrotliFlag) != 0)
                throw new InvalidDataException("The FBF file uses unsupported feature flags.");

            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            ValidateDimensions(width, height);
            int pixelCount = checked(width * height);

            Vector2 framePosition = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            int fps = reader.ReadInt32();
            int layerCount = reader.ReadInt32();
            int frameCount = reader.ReadInt32();
            int keyframeInterval = reader.ReadInt32();
            string projectName = ReadString(reader);
            List<AnimationLayer> layers = ReadLayerDefinitions(reader, layerCount);
            long indexOffset = reader.ReadInt64();

            if (fps <= 0 || layerCount <= 0 || layerCount > MaxLayerCount ||
                frameCount <= 0 || frameCount > MaxFrameCount || keyframeInterval <= 0)
                throw new InvalidDataException("The FBF project header contains invalid values.");
            if (string.IsNullOrWhiteSpace(projectName))
                throw new InvalidDataException("The FBF project name is missing.");
            try
            {
                projectName = SaveService.ValidateProjectName(projectName);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException("The FBF project name is invalid.", ex);
            }
            ValidateResourceBudget(width, height, layerCount, frameCount);

            long[] frameOffsets = ReadFrameIndex(reader, stream, indexOffset, frameCount);
            var loadedFrames = new List<Frame>(frameCount);
            Dictionary<int, uint>[] currentLayers = CreateEmptyLayers(layerCount);

            try
            {
                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    stream.Position = frameOffsets[frameIndex];
                    ReadFrameChunk(reader, frameIndex, pixelCount, currentLayers);

                    Frame frame = new Frame(framePosition, new Vector2(width, height), layers);
                    RestoreLayers(frame, currentLayers, layers);
                    loadedFrames.Add(frame);
                }

                var animation = new Animation.Animation(projectName, layers) { fps = fps };
                animation.LoadFrames(loadedFrames, framePosition, new Vector2(width, height));
                return animation;
            }
            catch
            {
                foreach (Frame frame in loadedFrames)
                    frame.Dispose();
                throw;
            }
        }

        private static void WriteFrameChunk(BinaryWriter writer, int frameIndex, FrameKind kind,
            Dictionary<int, uint>[] previousLayers, Dictionary<int, uint>[] currentLayers)
        {
            WriteBytes(writer, FrameMagic);
            writer.Write(frameIndex);
            writer.Write((byte)kind);

            if (kind == FrameKind.SameAsPrevious)
            {
                writer.Write(0);
                writer.Write(0);
                return;
            }

            using var uncompressedStream = new MemoryStream();
            using (var payloadWriter = new BinaryWriter(uncompressedStream, Encoding.UTF8, leaveOpen: true))
            {
                for (int layerIndex = 0; layerIndex < currentLayers.Length; layerIndex++)
                {
                    List<PixelChange> changes = kind == FrameKind.Keyframe
                        ? CreateKeyframeChanges(currentLayers[layerIndex])
                        : CreateDeltaChanges(previousLayers[layerIndex], currentLayers[layerIndex]);

                    payloadWriter.Write(changes.Count);
                    foreach (PixelChange change in changes)
                    {
                        WriteVarUInt(payloadWriter, (uint)change.Index);
                        payloadWriter.Write(change.PackedColor);
                    }
                }
            }

            byte[] uncompressed = uncompressedStream.ToArray();
            if (uncompressed.Length > MaxFramePayloadBytes)
                throw new InvalidDataException("The frame is too large for the FBF v1 payload limit.");

            using var compressedStream = new MemoryStream();
            using (var brotli = new BrotliStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
                brotli.Write(uncompressed, 0, uncompressed.Length);

            byte[] compressed = compressedStream.ToArray();
            if (compressed.Length > MaxFramePayloadBytes)
                throw new InvalidDataException("The compressed frame exceeds the FBF v1 payload limit.");

            writer.Write(uncompressed.Length);
            writer.Write(compressed.Length);
            writer.Write(compressed);
        }

        private static void ReadFrameChunk(BinaryReader reader, int expectedFrameIndex, int pixelCount,
            Dictionary<int, uint>[] currentLayers)
        {
            ExpectBytes(reader, FrameMagic, "frame chunk signature");
            int frameIndex = reader.ReadInt32();
            FrameKind kind = (FrameKind)reader.ReadByte();
            int uncompressedLength = reader.ReadInt32();
            int compressedLength = reader.ReadInt32();

            if (frameIndex != expectedFrameIndex)
                throw new InvalidDataException("The FBF frame index is inconsistent.");
            if (!Enum.IsDefined(kind))
                throw new InvalidDataException("The FBF frame type is invalid.");

            if (kind == FrameKind.SameAsPrevious)
            {
                if (expectedFrameIndex == 0 || uncompressedLength != 0 || compressedLength != 0)
                    throw new InvalidDataException("The FBF identical-frame record is invalid.");
                return;
            }

            if (uncompressedLength <= 0 || uncompressedLength > MaxFramePayloadBytes ||
                compressedLength <= 0 || compressedLength > MaxFramePayloadBytes ||
                compressedLength > reader.BaseStream.Length - reader.BaseStream.Position)
                throw new InvalidDataException("The FBF frame chunk length is invalid.");

            byte[] compressed = reader.ReadBytes(compressedLength);
            byte[] payload = new byte[uncompressedLength];
            using (var compressedStream = new MemoryStream(compressed, writable: false))
            using (var brotli = new BrotliStream(compressedStream, CompressionMode.Decompress))
            {
                int totalRead = 0;
                while (totalRead < payload.Length)
                {
                    int read = brotli.Read(payload, totalRead, payload.Length - totalRead);
                    if (read == 0)
                        throw new InvalidDataException("The FBF frame payload ended unexpectedly.");
                    totalRead += read;
                }

                if (brotli.ReadByte() != -1)
                    throw new InvalidDataException("The FBF frame payload exceeds its declared length.");
            }

            using var payloadStream = new MemoryStream(payload, writable: false);
            using var payloadReader = new BinaryReader(payloadStream);

            if (kind == FrameKind.Keyframe)
            {
                foreach (Dictionary<int, uint> layer in currentLayers)
                    layer.Clear();
            }

            for (int layerIndex = 0; layerIndex < currentLayers.Length; layerIndex++)
            {
                int changeCount = payloadReader.ReadInt32();
                if (changeCount < 0 || changeCount > pixelCount * 2L)
                    throw new InvalidDataException("The FBF layer change count is invalid.");

                for (int changeIndex = 0; changeIndex < changeCount; changeIndex++)
                {
                    uint pixelIndex = ReadVarUInt(payloadReader);
                    uint packedColor = payloadReader.ReadUInt32();
                    if (pixelIndex >= pixelCount)
                        throw new InvalidDataException("The FBF layer contains an invalid pixel index.");

                    if (packedColor == 0)
                        currentLayers[layerIndex].Remove((int)pixelIndex);
                    else
                        currentLayers[layerIndex][(int)pixelIndex] = packedColor;
                }
            }

            if (payloadStream.Position != payloadStream.Length)
                throw new InvalidDataException("The FBF frame payload contains trailing data.");
        }

        private static long[] ReadFrameIndex(BinaryReader reader, Stream stream, long indexOffset, int frameCount)
        {
            if (indexOffset <= 0 || indexOffset > stream.Length - 16)
                throw new InvalidDataException("The FBF frame index offset is invalid.");

            stream.Position = indexOffset;
            ExpectBytes(reader, IndexMagic, "frame index signature");
            int indexedFrameCount = reader.ReadInt32();
            if (indexedFrameCount != frameCount)
                throw new InvalidDataException("The FBF frame index count is inconsistent.");

            var offsets = new long[frameCount];
            long previousOffset = 0;
            for (int i = 0; i < frameCount; i++)
            {
                long offset = reader.ReadInt64();
                if (offset <= previousOffset || offset >= indexOffset)
                    throw new InvalidDataException("The FBF frame index contains an invalid offset.");
                offsets[i] = offset;
                previousOffset = offset;
            }

            ExpectBytes(reader, FooterMagic, "footer signature");
            if (reader.ReadInt64() != indexOffset)
                throw new InvalidDataException("The FBF footer index offset is inconsistent.");

            return offsets;
        }

        private static Dictionary<int, uint>[] SnapshotLayers(Frame frame, IReadOnlyList<AnimationLayer> definitions)
        {
            var layers = CreateEmptyLayers(definitions.Count);
            for (int layerIndex = 0; layerIndex < definitions.Count; layerIndex++)
            {
                foreach (var pixel in frame.GetSparseLayerPixels(definitions[layerIndex].Id))
                    layers[layerIndex][pixel.Key] = pixel.Value.PackedValue;
            }
            return layers;
        }

        private static void RestoreLayers(Frame frame, Dictionary<int, uint>[] layers, IReadOnlyList<AnimationLayer> definitions)
        {
            for (int layerIndex = 0; layerIndex < definitions.Count; layerIndex++)
            {
                Color[] pixels = new Color[frame.width * frame.height];
                foreach (var pixel in layers[layerIndex])
                {
                    pixels[pixel.Key] = new Color { PackedValue = pixel.Value };
                }
                frame.SetLayerPixels(definitions[layerIndex].Id, pixels, ignoreLock: true);
            }
        }

        private static List<PixelChange> CreateKeyframeChanges(Dictionary<int, uint> current)
        {
            return current.Select(pixel => new PixelChange(pixel.Key, pixel.Value)).ToList();
        }

        private static List<PixelChange> CreateDeltaChanges(Dictionary<int, uint> previous, Dictionary<int, uint> current)
        {
            var changes = new List<PixelChange>();
            foreach (var pixel in current)
            {
                if (!previous.TryGetValue(pixel.Key, out uint previousColor) || previousColor != pixel.Value)
                    changes.Add(new PixelChange(pixel.Key, pixel.Value));
            }

            foreach (var pixel in previous)
            {
                if (!current.ContainsKey(pixel.Key))
                    changes.Add(new PixelChange(pixel.Key, 0));
            }

            return changes;
        }

        private static bool LayersEqual(Dictionary<int, uint>[] left, Dictionary<int, uint>[] right)
        {
            if (left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i].Count != right[i].Count)
                    return false;
                foreach (var pixel in left[i])
                {
                    if (!right[i].TryGetValue(pixel.Key, out uint color) || color != pixel.Value)
                        return false;
                }
            }
            return true;
        }

        private static Dictionary<int, uint>[] CreateEmptyLayers(int count)
        {
            return Enumerable.Range(0, count).Select(_ => new Dictionary<int, uint>()).ToArray();
        }

        private static List<AnimationLayer> ReadLayerDefinitions(BinaryReader reader, int count)
        {
            if (count <= 0 || count > MaxLayerCount)
                throw new InvalidDataException("The FBF layer count is invalid or unsupported.");
            var layers = new List<AnimationLayer>(count);
            var ids = new HashSet<Guid>();
            for (int i = 0; i < count; i++)
            {
                byte[] idBytes = reader.ReadBytes(16);
                if (idBytes.Length != 16) throw new EndOfStreamException("The FBF layer ID is incomplete.");
                Guid id = new Guid(idBytes);
                string name = ReadString(reader);
                bool isVisible = reader.ReadBoolean();
                bool isLocked = reader.ReadBoolean();
                if (id == Guid.Empty || !ids.Add(id) || string.IsNullOrWhiteSpace(name))
                    throw new InvalidDataException("The FBF layer metadata is invalid.");
                layers.Add(new AnimationLayer(name, id, isVisible, isLocked));
            }
            return layers;
        }

        private static void ValidateDimensions(int width, int height)
        {
            if (width <= 0 || height <= 0 || width > MaxDimension || height > MaxDimension)
                throw new InvalidDataException("The project canvas dimensions are invalid or unsupported.");
            _ = checked(width * height);
        }

        private static void ValidateResourceBudget(int width, int height, int layerCount, int frameCount)
        {
            if (layerCount <= 0 || layerCount > MaxLayerCount ||
                frameCount <= 0 || frameCount > MaxFrameCount)
                throw new InvalidDataException("The project resource counts are invalid or unsupported.");

            long decodedPixelSlots = (long)width * height * layerCount * frameCount;
            if (decodedPixelSlots > MaxDecodedPixelSlots)
                throw new InvalidDataException("The project exceeds the supported decoded pixel budget.");
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            if (bytes.Length > MaxStringBytes)
                throw new InvalidDataException("The project name is too long for the FBF format.");
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private static string ReadString(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            if (length < 0 || length > MaxStringBytes || length > reader.BaseStream.Length - reader.BaseStream.Position)
                throw new InvalidDataException("The FBF string length is invalid.");
            return Encoding.UTF8.GetString(reader.ReadBytes(length));
        }

        private static void WriteVarUInt(BinaryWriter writer, uint value)
        {
            while (value >= 0x80)
            {
                writer.Write((byte)((value & 0x7f) | 0x80));
                value >>= 7;
            }
            writer.Write((byte)value);
        }

        private static uint ReadVarUInt(BinaryReader reader)
        {
            uint value = 0;
            for (int shift = 0; shift < 35; shift += 7)
            {
                byte current = reader.ReadByte();
                value |= (uint)(current & 0x7f) << shift;
                if ((current & 0x80) == 0)
                    return value;
            }
            throw new InvalidDataException("The FBF variable-length integer is invalid.");
        }

        private static void WriteBytes(BinaryWriter writer, byte[] bytes) => writer.Write(bytes);

        private static void ExpectBytes(BinaryReader reader, byte[] expected, string description)
        {
            byte[] actual = reader.ReadBytes(expected.Length);
            if (!actual.AsSpan().SequenceEqual(expected))
                throw new InvalidDataException($"Invalid {description}.");
        }
    }
}

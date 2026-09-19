using FrameByFrame.src.UI;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Export;
using FrameByFrame.src.Engine.Audio;
using Microsoft.Xna.Framework;
using System.IO;
using System.Text;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

Rectangle wide = UILayoutEngine.FitAspect(new Rectangle(0, 0, 800, 800), 1.5f);
Assert(wide.Width == 800 && wide.Height == 533, "Aspect fitting should constrain height.");
Assert(wide.X == 0 && wide.Y == 134, "Aspect fitting should center the result.");

Rectangle tall = UILayoutEngine.FitAspect(new Rectangle(10, 20, 900, 300), 1.5f);
Assert(tall.Width == 450 && tall.Height == 300, "Aspect fitting should constrain width.");
Assert(tall.X == 235 && tall.Y == 20, "Aspect fitting should preserve the container origin while centering.");

var horizontal = UILayoutEngine.Stack(new Rectangle(0, 0, 320, 40), UIAxis.Horizontal, 3, 10);
Assert(horizontal.Count == 3, "Stack should create the requested item count.");
Assert(horizontal[0].Width == 100 && horizontal[1].X == 110 && horizontal[2].X == 220,
    "Horizontal stack should distribute width and spacing consistently.");

var vertical = UILayoutEngine.Stack(new Rectangle(5, 7, 80, 220), UIAxis.Vertical, 2, 20);
Assert(vertical[0] == new Rectangle(5, 7, 80, 100), "Vertical stack should position its first item.");
Assert(vertical[1] == new Rectangle(5, 127, 80, 100), "Vertical stack should position its second item.");

var grid = UILayoutEngine.Grid(new Rectangle(0, 0, 620, 420), 6, 3, 10);
Assert(grid.Count == 6 && grid[0] == new Rectangle(0, 0, 200, 205),
    "Grid layout should size the first project card consistently.");
Assert(grid[2].X == 420 && grid[3].Y == 215,
    "Grid layout should place six project cards in two rows of three.");

UIBox box = new(new Rectangle(10, 20, 100, 80), 8);
Assert(box.Content == new Rectangle(18, 28, 84, 64), "UIBox padding should produce stable content bounds.");

TestElement element = new();
element.Arrange(new Rectangle(12, 18, 140, 44));
Assert(element.Bounds == new Rectangle(12, 18, 140, 44), "UI elements should retain arranged bounds.");
Assert(element.Measure(new Point(100, 30)) == new Point(100, 30), "UI measurement should respect available space.");

string autosaveSettings = Path.Combine(Path.GetTempPath(), $"framebyframe-autosave-{Guid.NewGuid():N}.json");
try
{
    var autosave = new AutosaveService(autosaveSettings);
    Assert(autosave.IsEnabled, "Autosave should be enabled by default.");
    autosave.SetEnabled(false);
    var reloadedAutosave = new AutosaveService(autosaveSettings);
    Assert(!reloadedAutosave.IsEnabled, "The autosave preference should persist.");
}
finally
{
    File.Delete(autosaveSettings);
}

using (var animation = new Animation("Layer model test"))
{
    Assert(!animation.IsCanvasBackgroundTransparent && animation.CanvasBackgroundColor == Color.White,
        "Animations should preserve the existing solid white canvas default.");
    animation.SetCanvasBackgroundTransparent(true);
    Assert(animation.IsCanvasBackgroundTransparent,
        "Canvas backgrounds should support transparency.");
    animation.SetCanvasBackgroundColor(Color.CornflowerBlue);
    Assert(!animation.IsCanvasBackgroundTransparent && animation.CanvasBackgroundColor == Color.CornflowerBlue,
        "Choosing a solid canvas color should disable transparency and retain the chosen color.");
    Assert(animation.Layers.Count == 3, "Animations should start with three editable layers.");
    AnimationLayer added = animation.AddLayer("Highlights");
    Assert(animation.Layers[0] == added && animation.SelectedLayerId == added.Id,
        "New layers should be inserted and selected by stable ID.");
    Assert(animation.RenameLayer(added.Id, "Lighting") && added.Name == "Lighting",
        "Layers should be renameable without changing identity.");
    Assert(animation.SetLayerVisibility(added.Id, false) && !added.IsVisible,
        "Layer visibility should be configurable through the animation model.");
    Assert(animation.MoveLayer(added.Id, animation.Layers.Count - 1) && animation.Layers[^1].Id == added.Id,
        "Layers should support explicit reordering.");
    Assert(animation.RemoveLayer(added.Id) && animation.Layers.Count == 3,
        "Removing a layer should update the project layer collection.");

    var audio = new AudioTrack("beat.MP3", ".MP3", [1, 2, 3], 12, 2.5, new byte[4]);
    Assert(animation.AddAudioTrack(audio) && animation.AudioTracks.Count == 1,
        "Animations should accept embedded audio tracks.");
    Assert(audio.StartFrame == 0,
        "Audio placement should clamp to the available animation timeline.");
    Assert(animation.SetAudioTrackVolume(audio.Id, -.5f) && audio.Volume == 0,
        "Audio volume should clamp to silence.");
    Assert(animation.SetAudioTrackVolume(audio.Id, 2f) && audio.Volume == 1,
        "Audio volume should clamp to full volume.");
    Assert(animation.SetAudioTrackMuted(audio.Id, true) && audio.IsMuted,
        "Audio tracks should support mute state changes.");
    Assert(animation.RemoveAudioTrack(audio.Id) && animation.AudioTracks.Count == 0,
        "Audio tracks should be removable from the animation.");
}

var persistedAudio = new AudioTrack("music.ogg", ".ogg", [10, 20, 30, 40], 3, 1.25,
    id: Guid.NewGuid(), volume: .6f, isMuted: true);
using (var audioStream = new MemoryStream())
{
    using (var writer = new BinaryWriter(audioStream, Encoding.UTF8, leaveOpen: true))
        FbfProjectFile.WriteAudioTracks(writer, [persistedAudio], frameCount: 10);
    audioStream.Position = 0;
    using var reader = new BinaryReader(audioStream, Encoding.UTF8, leaveOpen: true);
    AudioTrack loadedAudio = FbfProjectFile.ReadAudioTracks(reader, frameCount: 10).Single();
    Assert(loadedAudio.Id == persistedAudio.Id && loadedAudio.SourceName == "music.ogg" &&
           loadedAudio.StartFrame == 3 && Math.Abs(loadedAudio.Volume - .6f) < .001f &&
           loadedAudio.IsMuted && loadedAudio.EncodedData.SequenceEqual(persistedAudio.EncodedData),
        "FBF audio records should preserve identity, timing, controls, and encoded bytes.");
}

using (var invalidAudioStream = new MemoryStream())
{
    using (var writer = new BinaryWriter(invalidAudioStream, Encoding.UTF8, leaveOpen: true))
        writer.Write(65);
    invalidAudioStream.Position = 0;
    using var reader = new BinaryReader(invalidAudioStream, Encoding.UTF8, leaveOpen: true);
    bool rejected = false;
    try { FbfProjectFile.ReadAudioTracks(reader, frameCount: 10); }
    catch (InvalidDataException) { rejected = true; }
    Assert(rejected, "FBF loading should reject an excessive audio-track count before allocation.");
}

using (var animation = new Animation("Audio export filter test") { fps = 10 })
{
    var first = new AudioTrack("first.wav", ".wav", [1], 5, 2, volume: .5f);
    var second = new AudioTrack("second.wav", ".wav", [2], 10, 2, volume: 1f);
    string filter = SaveService.BuildAudioFilter(animation, [first, second], 1, 3, 2);
    Assert(filter.Contains("adelay=500:all=1") && filter.Contains("adelay=1000:all=1") &&
           filter.Contains("amix=inputs=2:normalize=0") && filter.Contains("atrim=start=1:end=3"),
        "Video audio filters should retain track offsets, mixing, and export-range trimming.");
}

var timedAudio = new AudioTrack("timing.wav", ".wav", [1], 10, 2,
    new byte[AudioTrack.BytesPerSecond * 2]);
Assert(AudioPlaybackController.CalculatePcmOffset(timedAudio, .5, 10) == -1,
    "Audio playback should remain silent before a delayed track starts.");
Assert(AudioPlaybackController.CalculatePcmOffset(timedAudio, 1.5, 10) == AudioTrack.BytesPerSecond / 2,
    "Audio playback should seek to the sample matching the animation playhead.");
Assert(AudioPlaybackController.CalculatePcmOffset(timedAudio, 3, 10) == -1,
    "Audio playback should stop after the source duration.");

UISlider slider = new(1, 50, 5);
slider.SetValue(-20);
Assert(slider.Value == 1, "Sliders should clamp values to their minimum.");
slider.SetValue(200);
Assert(slider.Value == 50, "Sliders should clamp values to their maximum.");

Color X = Color.Black;
Color O = Color.Transparent;
Color F = Color.Orange;
Color[] fillPixels =
{
    O, O, X, O,
    O, X, X, O,
    O, O, X, O,
};
Frame.FloodFillPixels(fillPixels, 4, 3, 0, 0, F);
Assert(fillPixels[0] == F && fillPixels[4] == F && fillPixels[8] == F,
    "Flood fill should replace the connected target region.");
Assert(fillPixels[3] == O && fillPixels[7] == O && fillPixels[11] == O,
    "Flood fill should not cross a separating boundary.");
Assert(fillPixels[2] == X && fillPixels[5] == X,
    "Flood fill should preserve boundary colors.");

Assert(!SaveService.FormatSupportsTransparency(ExportFormat.Mp4) &&
       !SaveService.FormatSupportsTransparency(ExportFormat.Mov),
    "Video exports should flatten transparent canvas backgrounds.");
Assert(SaveService.FormatSupportsTransparency(ExportFormat.Gif) &&
       SaveService.FormatSupportsTransparency(ExportFormat.PngSequence) &&
       SaveService.FormatSupportsTransparency(ExportFormat.SpriteSheet),
    "Alpha-capable export formats should retain canvas transparency.");

Console.WriteLine("FrameByFrame UI contract tests passed.");

sealed class TestElement : UIElement { }

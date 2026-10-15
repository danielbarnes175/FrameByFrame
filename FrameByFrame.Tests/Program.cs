using FrameByFrame.src.UI;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Export;
using FrameByFrame.src.Engine.Audio;
using FrameByFrame.src.Engine.Selection;
using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Scenes;
using Microsoft.Xna.Framework;
using System.IO;
using System.Text;
using System.Text.Json;

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

UITextMetrics displayText = UITheme.Typography(UITextRole.Display);
UITextMetrics bodyText = UITheme.Typography(UITextRole.Body);
UITextMetrics captionText = UITheme.Typography(UITextRole.Caption);
Assert(displayText.Bold && displayText.Scale > bodyText.Scale,
    "Display text should be bolder and larger than body copy.");
Assert(!bodyText.Bold && !captionText.Bold && bodyText.Scale > captionText.Scale,
    "Body and supporting copy should use a readable regular-weight hierarchy.");

Rectangle tooltipViewport = new(8, 8, 304, 464);
Rectangle containedTooltip = UILayoutEngine.PlaceFloatingPanel(
    new Rectangle(300, 450, 40, 40), new Point(420, 600), tooltipViewport, preferBelow: true);
Assert(containedTooltip.X >= tooltipViewport.X && containedTooltip.Y >= tooltipViewport.Y &&
       containedTooltip.Right <= tooltipViewport.Right && containedTooltip.Bottom <= tooltipViewport.Bottom,
    "Floating text panels should remain contained at the minimum supported window size.");

Rectangle flippedTooltip = UILayoutEngine.PlaceFloatingPanel(
    new Rectangle(100, 420, 40, 32), new Point(160, 80), tooltipViewport, preferBelow: true);
Assert(flippedTooltip.Bottom <= 420,
    "A tooltip near the bottom edge should move above its anchor when space is available.");

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

Assert(ProjectsScene.ResolvePageStart(6, -1, 14, 6) == 6 &&
       ProjectsScene.ResolvePageStart(12, -1, 14, 6) == 12,
    "Project pagination should retain the chosen page when no project is selected.");
Assert(ProjectsScene.ResolvePageStart(6, 2, 14, 6) == 0,
    "Project pagination should bring an explicitly selected project into view.");

UIBox box = new(new Rectangle(10, 20, 100, 80), 8);
Assert(box.Content == new Rectangle(18, 28, 84, 64), "UIBox padding should produce stable content bounds.");

TestElement element = new();
element.Arrange(new Rectangle(12, 18, 140, 44));
Assert(element.Bounds == new Rectangle(12, 18, 140, 44), "UI elements should retain arranged bounds.");
Assert(element.Measure(new Point(100, 30)) == new Point(100, 30), "UI measurement should respect available space.");

var canvasView = new FrameByFrame.src.Engine.CanvasView();
canvasView.Arrange(new Rectangle(100, 50, 800, 600), 400, 200);
Vector2 canvasPoint = new(75, 125);
Vector2 screenPoint = canvasView.CanvasToScreen(canvasPoint);
Assert(Vector2.Distance(canvasPoint, canvasView.ScreenToCanvas(screenPoint)) < .001f,
    "Canvas view transforms should round-trip drawing coordinates.");
canvasView.RotateBy(90);
screenPoint = canvasView.CanvasToScreen(canvasPoint);
Assert(Vector2.Distance(canvasPoint, canvasView.ScreenToCanvas(screenPoint)) < .001f,
    "Rotated canvas views should preserve inverse coordinate mapping.");
canvasView.SetZoom(100);
Assert(canvasView.Zoom == FrameByFrame.src.Engine.CanvasView.MaximumZoom,
    "Canvas view zoom should clamp to its supported maximum.");
Vector2 centerBeforePan = canvasView.Center;
canvasView.PanBy(new Vector2(25, -10));
Assert(canvasView.Center == centerBeforePan + new Vector2(25, -10),
    "Panning should move the canvas view by the mouse delta.");
Vector2 zoomAnchor = new(260, 190);
Vector2 anchoredCanvasPoint = canvasView.ScreenToCanvas(zoomAnchor);
canvasView.ZoomAt(zoomAnchor, -FrameByFrame.src.Engine.CanvasView.ZoomStep);
Assert(Vector2.Distance(zoomAnchor, canvasView.CanvasToScreen(anchoredCanvasPoint)) < .001f,
    "Mouse-wheel zoom should keep the canvas point beneath the cursor anchored.");
canvasView.Reset();
Assert(canvasView.Zoom == 1f && canvasView.RotationDegrees == 0f && canvasView.PanOffset == Vector2.Zero,
    "Reset should restore the default editor-only canvas view.");

object firstStrokeFrame = new();
object secondStrokeFrame = new();
Guid firstStrokeLayer = Guid.NewGuid();
Assert(!DrawingTools.HasEditTargetChanged(true, firstStrokeFrame, firstStrokeLayer,
           firstStrokeFrame, firstStrokeLayer) &&
       DrawingTools.HasEditTargetChanged(true, firstStrokeFrame, firstStrokeLayer,
           secondStrokeFrame, firstStrokeLayer) &&
       DrawingTools.HasEditTargetChanged(true, firstStrokeFrame, firstStrokeLayer,
           firstStrokeFrame, Guid.NewGuid()) &&
       !DrawingTools.HasEditTargetChanged(false, firstStrokeFrame, firstStrokeLayer,
           secondStrokeFrame, firstStrokeLayer),
    "Held drawing gestures should split into a new stroke when their frame or layer target changes.");

var feedback = new UIFeedbackState();
feedback.ShowBusy("Loading", "Opening a project...", 2f);
Assert(feedback.IsVisible && feedback.IsBusy && feedback.Progress == 1f,
    "Busy feedback should be visible and clamp determinate progress.");
feedback.Dismiss();
Assert(feedback.IsVisible, "Busy feedback should not be dismissible while work is active.");
feedback.Advance(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(250)));
Assert(feedback.SpinnerPhase > 0f, "Busy feedback should animate as updates advance.");
feedback.ShowError("Load failed", "The file is damaged.", "Choose another file.");
Assert(!feedback.IsBusy && feedback.Kind == UIFeedbackKind.Error &&
       feedback.ActionHint == "Choose another file.",
    "Error feedback should retain actionable recovery guidance.");
feedback.ShowErrorList("Projects skipped", ["one.fbf: invalid header", "two.fbf: truncated data"],
    "Repair or remove these projects.");
Assert(feedback.Details.Count == 2 && feedback.Details[0].StartsWith("one.fbf") &&
       string.IsNullOrEmpty(feedback.Message),
    "List feedback should retain every project-specific error for scrolling.");
feedback.ShowError("Load failed", "The file is damaged.", "Choose another file.");
Assert(feedback.Details.Count == 0,
    "Ordinary feedback should clear details from an earlier list.");
feedback.Dismiss();
Assert(!feedback.IsVisible, "Completed feedback should be dismissible.");

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

BrushPreset solidPen = BrushPreset.CreateSolidPen();
BrushPreset checker = BrushPreset.CreateCheckerDither();
Assert(BrushRasterizer.Sample(solidPen, 0, 0, 8, 0, 0) == byte.MaxValue &&
       BrushRasterizer.Sample(solidPen, -8, -8, 8, -8, -8) == 0 &&
       BrushRasterizer.Sample(checker, 0, 0, 8, 8, 8) !=
           BrushRasterizer.Sample(checker, 1, 0, 8, 9, 8) &&
       BrushRasterizer.Sample(checker, 0, 0, 8, 8, 8) ==
           BrushRasterizer.Sample(checker, -1, 0, 8, 8, 8) &&
       BrushRasterizer.Sample(checker, 0, 0, 32, 0, 0) ==
           BrushRasterizer.Sample(checker, 1, 0, 32, 1, 0),
    "Brush-tip masks should control the opacity and footprint of each stamp.");
byte builtInAlpha = checker.AlphaAt(0, 0);
Assert(checker.IsBuiltIn && !checker.SetAlpha(0, 0, (byte)(255 - builtInAlpha)) &&
       checker.AlphaAt(0, 0) == builtInAlpha,
    "Built-in brush presets should be immutable.");
Color halfPaint = BrushRasterizer.Apply(Color.Transparent, Color.Red, 128, erase: false);
Assert(halfPaint.R == 255 && halfPaint.A is >= 127 and <= 128,
    "Brush-tip alpha should blend paint into transparent pixels.");
Color halfErase = BrushRasterizer.Apply(Color.Blue, Color.Transparent, 128, erase: true);
Assert(halfErase.B == 255 && halfErase.A is >= 127 and <= 128,
    "The same brush-tip alpha should apply while erasing.");

string brushSettings = Path.Combine(Path.GetTempPath(), $"framebyframe-brushes-{Guid.NewGuid():N}.json");
try
{
    var brushes = new BrushPresetStore(brushSettings);
    Assert(brushes.Presets.Count >= 9 &&
           brushes.Presets.Any(preset => preset.Name == "Vertical Tone"),
        "The built-in library should expose a useful range of pen, texture, and tone presets.");
    Assert(File.Exists(Path.Combine(AppContext.BaseDirectory, "DefaultBrushes.json")) &&
           brushes.Presets.Take(9).All(preset => preset.IsBuiltIn),
        "The committed default library should load before custom brushes.");
    Assert(!brushes.Remove(brushes.Presets[0].Id),
        "Built-in brush presets should not be removable.");
    BrushPreset custom = brushes.Add(brushes.Presets[0]);
    custom.Name = "Custom stipple";
    custom.SetAlpha(0, 0, 73);
    byte[] sourceTip = (byte[])custom.TipAlpha.Clone();
    BrushPreset oil = brushes.Presets.Single(preset => preset.Name == "Oil");
    Guid oilId = oil.Id;
    oil.TipAlpha[0] = byte.MaxValue;
    brushes.Save();
    using (JsonDocument customFile = JsonDocument.Parse(File.ReadAllText(brushSettings)))
        Assert(customFile.RootElement.GetArrayLength() == 1 &&
               customFile.RootElement[0].GetProperty("Name").GetString() == "Custom stipple",
            "The custom library should persist only user-created brushes.");
    var reloadedBrushes = new BrushPresetStore(brushSettings);
    BrushPreset restoredOil = reloadedBrushes.Presets.Single(preset => preset.Name == "Oil");
    Assert(restoredOil.Id == oilId && restoredOil.IsBuiltIn &&
           restoredOil.TipAlpha.SequenceEqual(BrushPreset.CreateOil().TipAlpha),
        "Loading should repair built-in presets changed by older editable versions.");
    BrushPreset restored = reloadedBrushes.Presets.Single(preset => preset.Id == custom.Id);
    Assert(restored.Name == "Custom stipple" && restored.TipWidth == BrushPreset.DefaultTipSize &&
           restored.TipHeight == BrushPreset.DefaultTipSize && restored.TipAlpha.SequenceEqual(sourceTip),
        "Custom brush-tip masks should persist and reload with stable identity.");
    BrushPreset clone = restored.Clone("Clone");
    clone.SetAlpha(0, 0, 255);
    Assert(restored.AlphaAt(0, 0) == 73,
        "Cloned presets should own independent brush-tip masks.");
}
finally
{
    File.Delete(brushSettings);
}

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

Color[] selectionCanvas =
{
    Color.Red, Color.Green, Color.Transparent, Color.Transparent,
    Color.Blue, Color.Yellow, Color.Transparent, Color.Transparent,
    Color.Purple, Color.Orange, Color.Transparent, Color.Transparent
};
Vector2[] lasso =
{
    new(0, 0), new(2, 0), new(2, 1), new(1, 1), new(1, 3), new(0, 3)
};
PixelSelection selection = SelectionOperations.ExtractFreeform(selectionCanvas, 4, 3, lasso);
Assert(selection.Width == 2 && selection.Height == 3 &&
       selection.Pixels[0] == Color.Red && selection.Pixels[1] == Color.Green &&
       selection.Pixels[2] == Color.Blue && selection.Pixels[4] == Color.Purple &&
       !selection.Mask[3] && !selection.Mask[5],
    "Free-form extraction should retain only pixels enclosed by the lasso.");
Assert(selection.Contains(new Vector2(.5f, 2.5f)) && !selection.Contains(new Vector2(1.5f, 2.5f)),
    "Selection hit testing should follow the free-form mask rather than its bounds.");
PixelSelection rotatedSelection = SelectionOperations.RotateClockwise(selection);
Assert(rotatedSelection.Width == 3 && rotatedSelection.Height == 2 &&
       rotatedSelection.Pixels[0] == Color.Purple && rotatedSelection.Pixels[2] == Color.Red &&
       rotatedSelection.Pixels[5] == Color.Green &&
       rotatedSelection.Mask[0] && rotatedSelection.Mask[1] && rotatedSelection.Mask[2] &&
       !rotatedSelection.Mask[3] && !rotatedSelection.Mask[4] && rotatedSelection.Mask[5],
    "Selection rotation should turn the free-form mask and its pixels together.");
PixelSelection resizedSelection = SelectionOperations.Resize(selection, 4, 6);
Assert(resizedSelection.Pixels[0] == Color.Red && resizedSelection.Pixels[3] == Color.Green &&
       resizedSelection.Pixels[20] == Color.Purple && !resizedSelection.Mask[^1],
    "Selection resizing should preserve the free-form mask through nearest-neighbor scaling.");
SelectionOperations.Clear(selectionCanvas, 4, 3, selection);
PixelSelection movedSelection = SelectionOperations.Move(selection, 2, 0);
SelectionOperations.Composite(selectionCanvas, 4, 3, movedSelection);
Assert(selectionCanvas[0] == Color.Transparent && selectionCanvas[2] == Color.Red &&
       selectionCanvas[3] == Color.Green && selectionCanvas[5] == Color.Yellow &&
       selectionCanvas[9] == Color.Orange && selectionCanvas[10] == Color.Purple,
    "Moving a free-form selection should preserve pixels outside the lasso's mask.");

Color[] pasteTarget = { Color.Blue, Color.Green, Color.Yellow };
PixelSelection clipboardSelection = new(new Rectangle(0, 0, 3, 1),
    [Color.Transparent, Color.White, Color.Red]);
PixelSelection preparedPaste = SelectionOperations.PrepareForPaste(clipboardSelection);
SelectionOperations.Composite(pasteTarget, 3, 1, preparedPaste);
Assert(pasteTarget[0] == Color.Blue && pasteTarget[1] == Color.Green && pasteTarget[2] == Color.Red,
    "Pasting should preserve existing artwork beneath transparent and white clipboard pixels.");
Assert(preparedPaste.Mask[0] && preparedPaste.Mask[1] && preparedPaste.Mask[2] &&
       preparedPaste.Pixels[0] == Color.Transparent && preparedPaste.Pixels[1] == Color.Transparent,
    "A pasted selection should retain its full mask while treating transparent and white pixels as empty content.");
Color[] transformTarget = { Color.Transparent, Color.Transparent, Color.Transparent };
SelectionOperations.Composite(transformTarget, 3, 1, clipboardSelection);
Assert(transformTarget[1] == Color.White,
    "Selection transforms should preserve intentionally drawn white pixels even though paste treats white as empty.");

Color[] repeatedMoveTarget = { Color.Blue, Color.Green, Color.Yellow, Color.Orange };
PixelSelection floatingSelection = new(new Rectangle(0, 0, 1, 1), [Color.Red]);
PixelSelection selectionUnderlay = SelectionOperations.Capture(repeatedMoveTarget, 4, 1, floatingSelection);
SelectionOperations.Composite(repeatedMoveTarget, 4, 1, floatingSelection);
SelectionOperations.Restore(repeatedMoveTarget, 4, 1, selectionUnderlay);
floatingSelection = SelectionOperations.Move(floatingSelection, 1, 0);
selectionUnderlay = SelectionOperations.Capture(repeatedMoveTarget, 4, 1, floatingSelection);
SelectionOperations.Composite(repeatedMoveTarget, 4, 1, floatingSelection);
SelectionOperations.Restore(repeatedMoveTarget, 4, 1, selectionUnderlay);
floatingSelection = SelectionOperations.Move(floatingSelection, 2, 0);
selectionUnderlay = SelectionOperations.Capture(repeatedMoveTarget, 4, 1, floatingSelection);
SelectionOperations.Composite(repeatedMoveTarget, 4, 1, floatingSelection);
Assert(repeatedMoveTarget[0] == Color.Blue && repeatedMoveTarget[1] == Color.Green &&
       repeatedMoveTarget[2] == Color.Red && repeatedMoveTarget[3] == Color.Orange,
    "Moving a floating selection repeatedly should restore the artwork beneath each previous position.");

PixelSelection offCanvasSelection = new(new Rectangle(-1, 0, 2, 1), [Color.Red, Color.Green]);
Color[] offCanvasTarget = { Color.Blue, Color.Yellow };
PixelSelection offCanvasUnderlay = SelectionOperations.Capture(offCanvasTarget, 2, 1, offCanvasSelection);
SelectionOperations.Composite(offCanvasTarget, 2, 1, offCanvasSelection);
Assert(offCanvasTarget[0] == Color.Green && offCanvasTarget[1] == Color.Yellow,
    "An out-of-bounds selection should composite only the portion inside the canvas.");
SelectionOperations.Restore(offCanvasTarget, 2, 1, offCanvasUnderlay);
Assert(offCanvasTarget[0] == Color.Blue && offCanvasTarget[1] == Color.Yellow,
    "Restoring an out-of-bounds selection should preserve its in-canvas underlay.");
Color[] reenteredTarget = { Color.Transparent, Color.Transparent };
SelectionOperations.Composite(reenteredTarget, 2, 1, SelectionOperations.Move(offCanvasSelection, 0, 0));
Assert(reenteredTarget[0] == Color.Red && reenteredTarget[1] == Color.Green,
    "Selected pixels outside the canvas should remain available when the selection moves back in bounds.");

Assert(!SaveService.FormatSupportsTransparency(ExportFormat.Mp4) &&
       !SaveService.FormatSupportsTransparency(ExportFormat.Mov),
    "Video exports should flatten transparent canvas backgrounds.");
Assert(SaveService.FormatSupportsTransparency(ExportFormat.Gif) &&
       SaveService.FormatSupportsTransparency(ExportFormat.PngSequence) &&
       SaveService.FormatSupportsTransparency(ExportFormat.SpriteSheet),
    "Alpha-capable export formats should retain canvas transparency.");

var exportFrame = new SaveService.ExportFrameSnapshot([
    new SaveService.ExportLayerSnapshot([new KeyValuePair<int, Color>(0, Color.Red)]),
    new SaveService.ExportLayerSnapshot([new KeyValuePair<int, Color>(0, Color.Blue)])
]);
var exportSnapshot = new SaveService.ExportSnapshot(1, 1, 12, 0, Color.White,
    [exportFrame], Array.Empty<AudioTrack>());
byte[] exportedPixel = SaveService.ComposeFramePixels(exportSnapshot, exportFrame);
Assert(exportedPixel.SequenceEqual(new byte[] { 255, 0, 0, 255 }),
    "Background export compositing should preserve the same top-to-bottom layer order as the canvas.");

string asyncExportRoot = Path.Combine(Path.GetTempPath(), $"framebyframe-worker-export-{Guid.NewGuid():N}");
string asyncProjectDirectory = Path.Combine(asyncExportRoot, "Worker export test");
try
{
    Directory.CreateDirectory(asyncProjectDirectory);
    var operation = new SaveService.ExportOperation(exportSnapshot, "Worker export test", asyncExportRoot,
        asyncProjectDirectory, ExportFormat.PngSequence);
    operation.WaitForCompletion();
    Assert(operation.IsComplete && !operation.IsFailed &&
           File.Exists(Path.Combine(asyncProjectDirectory, "Frame_0.png")),
        "PNG sequence encoding should complete successfully on the export worker.");
}
finally
{
    if (Directory.Exists(asyncExportRoot)) Directory.Delete(asyncExportRoot, recursive: true);
}

Console.WriteLine("FrameByFrame UI contract tests passed.");

sealed class TestElement : UIElement { }

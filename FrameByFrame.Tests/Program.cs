using FrameByFrame.src.UI;
using FrameByFrame.src.Engine.Animation;
using Microsoft.Xna.Framework;

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

UIBox box = new(new Rectangle(10, 20, 100, 80), 8);
Assert(box.Content == new Rectangle(18, 28, 84, 64), "UIBox padding should produce stable content bounds.");

TestElement element = new();
element.Arrange(new Rectangle(12, 18, 140, 44));
Assert(element.Bounds == new Rectangle(12, 18, 140, 44), "UI elements should retain arranged bounds.");
Assert(element.Measure(new Point(100, 30)) == new Point(100, 30), "UI measurement should respect available space.");

using (var animation = new Animation("Layer model test"))
{
    Assert(animation.Layers.Count == 3, "Animations should start with three editable layers.");
    AnimationLayer added = animation.AddLayer("Highlights");
    Assert(animation.Layers[0] == added && animation.SelectedLayerId == added.Id,
        "New layers should be inserted and selected by stable ID.");
    Assert(animation.RenameLayer(added.Id, "Lighting") && added.Name == "Lighting",
        "Layers should be renameable without changing identity.");
    Assert(animation.MoveLayer(added.Id, animation.Layers.Count - 1) && animation.Layers[^1].Id == added.Id,
        "Layers should support explicit reordering.");
    Assert(animation.RemoveLayer(added.Id) && animation.Layers.Count == 3,
        "Removing a layer should update the project layer collection.");
}

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

Console.WriteLine("FrameByFrame UI contract tests passed.");

sealed class TestElement : UIElement { }

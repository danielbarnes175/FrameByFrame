using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Export;
using FrameByFrame.src.UI.Components;
using Microsoft.Xna.Framework;
using Xunit;

public sealed class NavbarComponentTests
{
    [Fact]
    public void SaveCoordinatorCanRunWithoutConstructingNavbar()
    {
        string preferences = Path.Combine(Path.GetTempPath(), $"fbf-navbar-{Guid.NewGuid():N}.json");
        try
        {
            using Animation animation = new("Save coordinator test");
            animation.InitializeFrames(64, 64);
            using AutosaveService autosave = new(preferences);
            bool saved = false;
            NavbarSaveCoordinator coordinator = new(animation, autosave, null, _ => saved = true);

            coordinator.Request();

            Assert.True(saved);
            Assert.False(coordinator.IsPending);
            Assert.Empty(coordinator.Error);
        }
        finally
        {
            File.Delete(preferences);
        }
    }

    [Fact]
    public void SaveCoordinatorReportsInjectedSaveFailure()
    {
        string preferences = Path.Combine(Path.GetTempPath(), $"fbf-navbar-{Guid.NewGuid():N}.json");
        try
        {
            using Animation animation = new("Save coordinator failure test");
            animation.InitializeFrames(64, 64);
            using AutosaveService autosave = new(preferences);
            NavbarSaveCoordinator coordinator = new(animation, autosave, null,
                _ => throw new IOException("disk unavailable"));

            coordinator.Request();

            Assert.Contains("disk unavailable", coordinator.Error);
            Assert.False(coordinator.IsPending);
        }
        finally
        {
            File.Delete(preferences);
        }
    }

    [Theory]
    [InlineData(255, 0, 0)]
    [InlineData(0, 255, 0)]
    [InlineData(0, 0, 255)]
    [InlineData(80, 120, 200)]
    public void ColorConversionRoundTrips(byte red, byte green, byte blue)
    {
        Color source = new(red, green, blue);
        NavbarColorComponent.RgbToHsv(source, out float hue, out float saturation, out float value);

        Color result = NavbarColorComponent.HsvToRgb(hue, saturation, value);

        Assert.InRange(Math.Abs(result.R - source.R), 0, 1);
        Assert.InRange(Math.Abs(result.G - source.G), 0, 1);
        Assert.InRange(Math.Abs(result.B - source.B), 0, 1);
    }
}

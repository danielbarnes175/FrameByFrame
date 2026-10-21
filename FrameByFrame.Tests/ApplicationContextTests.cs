using FrameByFrame.src;
using FrameByFrame.src.Engine.Scenes;
using Xunit;

public sealed class ApplicationContextTests
{
    [Fact]
    public void ResizeClampsViewportAndUpdatesScale()
    {
        ApplicationContext context = new(1600, 900);

        context.Resize(800, 450);

        Assert.Equal(800, context.ScreenWidth);
        Assert.Equal(450, context.ScreenHeight);
        Assert.Equal(.5f, context.ScaleX);
        Assert.Equal(.5f, context.ScaleY);
    }

    [Fact]
    public void NavigationOwnsSceneLookupAndCurrentScene()
    {
        ApplicationContext context = new(320, 240);
        NavigationService navigation = new();
        TestScene scene = new(context, navigation);
        navigation.Register("test", scene);

        navigation.Navigate("test");

        Assert.Same(scene, navigation.Current);
        Assert.Same(scene, navigation.Get<TestScene>("test"));
    }

    private sealed class TestScene : BaseScene
    {
        public TestScene(IApplicationContext app, INavigationService navigation)
            : base(app, navigation) { }
    }
}

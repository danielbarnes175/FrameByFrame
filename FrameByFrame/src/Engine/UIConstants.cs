using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine
{
    public static class UIConstants
    {
        // Frame constants
        public const int DEFAULT_FRAME_WIDTH = 1200;
        public const int DEFAULT_FRAME_HEIGHT = 800;
        public const int MAX_BRUSH_SIZE = 30;
        public const int MIN_BRUSH_SIZE = 1;

        public static readonly Color DEBUG_TEXT = Color.White;
        public static readonly Color DEBUG_PERFORMANCE = Color.Red;
        public static readonly Color DEBUG_MEMORY = Color.Cyan;

        // Layer names
        public const string LAYER_1 = "_layer1";
        public const string LAYER_2 = "_layer2";
        public const string LAYER_3 = "_layer3";

        // Scene names
        public const string MENU_SCENE = "Menu Scene";
        public const string DRAWING_SCENE = "Drawing Scene";
        public const string PROJECTS_SCENE = "Projects Scene";
    }
}

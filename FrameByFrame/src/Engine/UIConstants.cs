using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine
{
    public static class UIConstants
    {
        // Screen layout constants
        public const int DEFAULT_SCREEN_WIDTH = 1600;
        public const int DEFAULT_SCREEN_HEIGHT = 900;
        public const float MIN_SCALE = 0.5f;
        
        // Frame constants
        public const int DEFAULT_FRAME_WIDTH = 1200;
        public const int DEFAULT_FRAME_HEIGHT = 800;
        public const int MAX_BRUSH_SIZE = 30;
        public const int MIN_BRUSH_SIZE = 1;
        
        // UI Layout
        public const int NAVBAR_HEIGHT = 50;
        public const int BUTTON_SPACING = 5;
        public const int MARGIN_SMALL = 10;
        public const int MARGIN_MEDIUM = 20;
        
        // Color palette
        public static readonly Color BACKGROUND_DARK = new Color(45, 45, 45);
        public static readonly Color UI_ORANGE = Color.Orange;
        
        // Layer names (to replace magic strings)
        public const string LAYER_1 = "_layer1";
        public const string LAYER_2 = "_layer2";
        public const string LAYER_3 = "_layer3";
        
        // Scene names
        public const string MENU_SCENE = "Menu Scene";
        public const string DRAWING_SCENE = "Drawing Scene";
        public const string SETTINGS_SCENE = "Settings Scene";
        public const string PROJECTS_SCENE = "Projects Scene";
    }
}
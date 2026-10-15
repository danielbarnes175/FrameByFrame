using System;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Selection;
using FrameByFrame.src.UI;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine
{
    public enum DrawingTool
    {
        Draw,
        Eraser,
        Fill,
        ColorPicker,
        Selection
    }

    /// <summary>Owns drawing-tool selection, brush settings, and pointer-driven tool behavior.</summary>
    public sealed class DrawingTools
    {
        private readonly Animation.Animation _animation;
        private bool _pixelEditActive;
        private int _brushSize = 5;
        private DrawingTool _selectedTool = DrawingTool.Draw;

        public DrawingTools(Animation.Animation animation)
        {
            _animation = animation ?? throw new ArgumentNullException(nameof(animation));
            Selection = new SelectionController(_animation);
        }

        public DrawingTool SelectedTool
        {
            get => _selectedTool;
            set
            {
                if (_selectedTool == DrawingTool.Selection && value != DrawingTool.Selection) Selection.Clear();
                _selectedTool = value;
            }
        }
        public SelectionController Selection { get; }

        public int BrushSize
        {
            get => _brushSize;
            set => _brushSize = Math.Clamp(value, UIConstants.MIN_BRUSH_SIZE, UIConstants.MAX_BRUSH_SIZE);
        }

        public void Update(Color selectedColor, Action<Color> selectColor, bool canDraw)
        {
            if (SelectedTool == DrawingTool.Selection)
            {
                EndStroke();
                Selection.Update(canDraw);
                return;
            }
            if (!GlobalParameters.GlobalMouse.LeftClickHold())
            {
                EndStroke();
                return;
            }

            if (!canDraw || UIPointerRouter.IsPointerBlocked() ||
                !_animation.IsScreenPointOnCanvas(GlobalParameters.GlobalMouse.newMousePos)) return;

            switch (SelectedTool)
            {
                case DrawingTool.Draw:
                    BeginStroke();
                    _animation.DrawOnCurrentLayer(selectedColor, BrushSize);
                    break;
                case DrawingTool.Eraser:
                    BeginStroke();
                    _animation.DrawOnCurrentLayer(Color.Transparent, BrushSize);
                    break;
                case DrawingTool.Fill when GlobalParameters.GlobalMouse.LeftClick():
                    _animation.BeginPixelEdit();
                    _animation.FillCurrentLayerAt(GlobalParameters.GlobalMouse.newMousePos, selectedColor);
                    _animation.CommitPixelEdit();
                    break;
                case DrawingTool.ColorPicker:
                    Color sampled = _animation.SampleVisibleColorAt(GlobalParameters.GlobalMouse.newMousePos);
                    if (sampled.A > 0) selectColor?.Invoke(sampled);
                    break;
            }
        }

        public void EndStroke()
        {
            if (_pixelEditActive) _animation.CommitPixelEdit();
            _pixelEditActive = false;
        }

        private void BeginStroke()
        {
            if (_pixelEditActive) return;
            _animation.BeginPixelEdit();
            _pixelEditActive = true;
        }
    }
}

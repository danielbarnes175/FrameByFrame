using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly BrushPresetStore _brushStore;
        private bool _pixelEditActive;
        private Frame _strokeFrame;
        private Guid _strokeLayerId;
        private DrawingTool _selectedTool = DrawingTool.Draw;
        private int _selectedBrushIndex;
        private int _brushSize = 5;

        public DrawingTools(Animation.Animation animation, BrushPresetStore brushStore = null)
        {
            _animation = animation ?? throw new ArgumentNullException(nameof(animation));
            Selection = new SelectionController(_animation);
            _brushStore = brushStore ?? new BrushPresetStore();
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

        public IReadOnlyList<BrushPreset> Brushes => _brushStore.Presets;
        public BrushPreset SelectedBrush => Brushes[_selectedBrushIndex];
        public int SelectedBrushIndex => _selectedBrushIndex;
        public bool CanEditSelectedBrush => !SelectedBrush.IsBuiltIn;
        public string BrushStoreError => _brushStore.LastError;

        public void SelectBrush(int index) =>
            _selectedBrushIndex = Math.Clamp(index, 0, Math.Max(0, Brushes.Count - 1));

        public void CreateBrush()
        {
            BrushPreset created = _brushStore.Add(SelectedBrush);
            _selectedBrushIndex = Brushes.ToList().FindIndex(candidate => candidate.Id == created.Id);
        }

        public void DeleteSelectedBrush()
        {
            if (!_brushStore.Remove(SelectedBrush.Id)) return;
            _selectedBrushIndex = Math.Clamp(_selectedBrushIndex, 0, Brushes.Count - 1);
        }

        public void SetBrushTipAlpha(int x, int y, byte alpha)
        {
            if (!SelectedBrush.SetAlpha(x, y, alpha)) return;
            _brushStore.Save();
        }

        public void FillBrushTip(byte alpha)
        {
            if (!SelectedBrush.Fill(alpha)) return;
            _brushStore.Save();
        }

        public void Update(Color selectedColor, Action<Color> selectColor, bool canDraw)
        {
            bool pointerHeld = GlobalParameters.GlobalMouse.LeftClickHold();
            if (HasEditTargetChanged(_pixelEditActive, _strokeFrame, _strokeLayerId,
                    _animation.CurrentFrame, _animation.SelectedLayerId))
                EndStroke();
            if (!pointerHeld)
                EndStroke();
            if (SelectedTool == DrawingTool.Selection)
            {
                EndStroke();
                Selection.Update(canDraw);
                return;
            }
            if (!pointerHeld) return;

            if (!canDraw || UIPointerRouter.IsPointerBlocked() ||
                !_animation.IsScreenPointOnCanvas(GlobalParameters.GlobalMouse.newMousePos)) return;

            switch (SelectedTool)
            {
                case DrawingTool.Draw:
                    BeginStroke();
                    _animation.DrawOnCurrentLayer(selectedColor, SelectedBrush, BrushSize, erase: false);
                    break;
                case DrawingTool.Eraser:
                    BeginStroke();
                    _animation.DrawOnCurrentLayer(selectedColor, SelectedBrush, BrushSize, erase: true);
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
            _strokeFrame = null;
            _strokeLayerId = Guid.Empty;
        }

        private void BeginStroke()
        {
            if (_pixelEditActive) return;
            _animation.BeginPixelEdit();
            _pixelEditActive = true;
            _strokeFrame = _animation.CurrentFrame;
            _strokeLayerId = _animation.SelectedLayerId;
        }

        internal static bool HasEditTargetChanged(bool editActive, object strokeFrame, Guid strokeLayerId,
            object currentFrame, Guid currentLayerId) => editActive &&
            (!ReferenceEquals(strokeFrame, currentFrame) || strokeLayerId != currentLayerId);
    }
}

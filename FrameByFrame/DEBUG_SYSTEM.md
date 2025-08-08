# Debug System Usage Guide

## Overview
The performance and memory monitors are now controlled by a comprehensive debug system that can be toggled at runtime.

## Debug Controls

### Keyboard Shortcuts:
- **F1**: Toggle Debug Mode (ON/OFF)
- **F2**: Toggle Performance Monitor (when debug mode is ON)
- **F3**: Toggle Memory Monitor (when debug mode is ON)

## Debug Modes

### 1. Automatic Debug Mode
- In DEBUG builds: Debug mode starts ON automatically
- In RELEASE builds: Debug mode starts OFF automatically
- Can be overridden at runtime using F1

### 2. Performance Monitor
- Shows FPS, average frame time, max/min frame times
- Updates every 60 frames for recent performance data
- Only active when debug mode is ON and performance monitoring is enabled
- Displays in red color at top-left corner

### 3. Memory Monitor
- Shows frame count, animation memory usage, and total system memory
- Only active when debug mode is ON and memory monitoring is enabled
- Displays in cyan color at bottom of DrawingScene
- Uses compressed storage metrics from the optimized frame system

### 4. Debug Help Overlay
- Shows current debug status and available controls
- Semi-transparent black background for readability
- Appears at bottom-left when debug mode is ON

## Implementation Details

### Zero Performance Impact When Disabled
- When debug mode is OFF, all monitoring code is bypassed
- No performance overhead from disabled debug features
- Texture creation and memory tracking only happens when needed

### Smart Resource Management
- Uses TextureManager for efficient background textures
- No memory leaks from debug overlays
- Proper cleanup when debug features are disabled

## Code Integration

The debug system is integrated into:
- `Main.cs`: Handles debug initialization and main overlays
- `DebugManager.cs`: Central control for all debug features
- `PerformanceMonitor.cs`: FPS and frame time tracking
- `MemoryMonitor.cs`: Memory usage tracking
- `DrawingScene.cs`: Scene-specific debug overlays

## Benefits

1. **Clean Release Builds**: No debug clutter in production
2. **Developer Friendly**: Easy runtime toggling for development
3. **Performance Focused**: Zero overhead when disabled
4. **Comprehensive Monitoring**: Both performance and memory tracking
5. **Visual Feedback**: Clear, color-coded debug information
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.UI
{
    public static class UIInteractionManager
    {
        private static List<Func<bool>> _activeUIElements = new List<Func<bool>>();
        private static bool _isUIBlocked = false;
        
        /// <summary>
        /// Register a UI element that can block input to the drawing scene
        /// </summary>
        /// <param name="isActiveFunc">Function that returns true if the UI element is currently being interacted with</param>
        public static void RegisterUIElement(Func<bool> isActiveFunc)
        {
            if (!_activeUIElements.Contains(isActiveFunc))
            {
                _activeUIElements.Add(isActiveFunc);
            }
        }
        
        /// <summary>
        /// Unregister a UI element
        /// </summary>
        /// <param name="isActiveFunc">Function to remove</param>
        public static void UnregisterUIElement(Func<bool> isActiveFunc)
        {
            _activeUIElements.Remove(isActiveFunc);
        }
        
        /// <summary>
        /// Clear all registered UI elements
        /// </summary>
        public static void Clear()
        {
            _activeUIElements.Clear();
            _isUIBlocked = false;
        }
        
        /// <summary>
        /// Update the UI interaction state - call this each frame
        /// </summary>
        public static void Update()
        {
            _isUIBlocked = false;
            
            // Check if any UI element is currently being interacted with
            foreach (var uiElementCheck in _activeUIElements)
            {
                try
                {
                    if (uiElementCheck())
                    {
                        _isUIBlocked = true;
                        break;
                    }
                }
                catch
                {
                    // Ignore disposed or invalid UI elements
                }
            }
        }
        
        /// <summary>
        /// Check if the UI is currently blocking input to the drawing scene
        /// </summary>
        /// <returns>True if UI is blocking input, false if drawing should be allowed</returns>
        public static bool IsUIBlocking()
        {
            return _isUIBlocked;
        }
        
        /// <summary>
        /// Manually set UI blocking state (for special cases)
        /// </summary>
        /// <param name="blocked">Whether UI should block input</param>
        public static void SetUIBlocked(bool blocked)
        {
            _isUIBlocked = blocked;
        }
        
        /// <summary>
        /// Check if mouse is over any UI element in the navbar area
        /// </summary>
        /// <param name="navbarBounds">The bounds of the navbar</param>
        /// <returns>True if mouse is over navbar area</returns>
        public static bool IsMouseOverNavbar(Rectangle navbarBounds)
        {
            Vector2 mousePos = GlobalParameters.GlobalMouse.newMousePos;
            return navbarBounds.Contains(mousePos);
        }
    }
}
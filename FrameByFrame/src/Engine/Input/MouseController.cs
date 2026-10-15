using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameByFrame.src.Engine.Input
{
    public class MouseController
    {
        public bool dragging, rightDrag;

        public Vector2 newMousePos, oldMousePos, firstMousePos;

        public MouseState newMouse, oldMouse, firstMouse;

        public MouseController()
        {
            dragging = false;

            newMouse = Mouse.GetState();
            oldMouse = newMouse;
            firstMouse = newMouse;

            newMousePos = new Vector2(newMouse.Position.X, newMouse.Position.Y);
            oldMousePos = new Vector2(newMouse.Position.X, newMouse.Position.Y);
            firstMousePos = new Vector2(newMouse.Position.X, newMouse.Position.Y);

            //screenLoc = new Vector2((int)(systemCursorPos.X/GlobalParameters.screenWidth), (int)(systemCursorPos.Y/GlobalParameters.screenHeight));

        }

        public void Update()
        {
            oldMouse = newMouse;
            newMouse = Mouse.GetState();

            oldMousePos = newMousePos;
            newMousePos = new Vector2(newMouse.X, newMouse.Y);
        }

        public bool LeftClick()
        {
            return (newMouse.LeftButton == ButtonState.Pressed && oldMouse.LeftButton == ButtonState.Released);
        }

        public bool LeftClickHold()
        {
            return (newMouse.LeftButton == ButtonState.Pressed);
        }

        public bool MiddleClick()
        {
            return newMouse.MiddleButton == ButtonState.Pressed && oldMouse.MiddleButton == ButtonState.Released;
        }

        public bool MiddleClickHold()
        {
            return newMouse.MiddleButton == ButtonState.Pressed;
        }

        public bool RightClick()
        {
            return newMouse.RightButton == ButtonState.Pressed && oldMouse.RightButton == ButtonState.Released;
        }

        public bool RightClickHold()
        {
            return newMouse.RightButton == ButtonState.Pressed;
        }

        public int ScrollDelta => newMouse.ScrollWheelValue - oldMouse.ScrollWheelValue;
    }
}

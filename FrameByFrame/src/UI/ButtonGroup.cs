using FrameByFrame.src.Services;
using FrameByFrame.src.UI.Components.Buttons;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace FrameByFrame.src.UI
{

    /**
     * Class that takes a list of radio buttons to then connect them to each other. Checks all the buttons, and is one is selected, it unselects the others.
     */
    public class ButtonGroup
    {
        public List<RadioButton> buttons;

        public ButtonGroup(List<RadioButton> buttons)
        {
            this.buttons = buttons;
        }

        public void Update()
        {
            foreach (RadioButton button in buttons)
            {
                if (CollisionService.CheckMouseCollision(button))
                {
                    button.isBeingMousedOver = true;
                }
                else
                {
                    button.isBeingMousedOver = false;
                }

                // If we select this button, set isSelected to true for this button, but false for all other buttons in this button group.
                if (button.isBeingMousedOver && GlobalParameters.GlobalMouse.LeftClickHold())
                {
                    button.isSelected = true;
                    foreach (RadioButton otherButton in buttons)
                    {
                        if (!otherButton.Equals(button))
                        {
                            otherButton.isSelected = false;
                        }
                    }
                }
                button.Update();
            }
        }

        public void Draw(Vector2 offset, Vector2 origin)
        {
            foreach (RadioButton button in buttons)
            {
                button.Draw(offset, origin);
            }
        }
    }
}

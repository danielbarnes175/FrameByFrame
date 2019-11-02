using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrameByFrame.src.Engine.Input.Keyboard;
using Microsoft.Xna.Framework.Input;

namespace FrameByFrame.src.Engine.Input
{
    public class KeyboardController
    {
        public KeyboardState newKeyboard, oldKeyboard;
        public List<KeyboardKeys> pressedKeys = new List<KeyboardKeys>(), previousPressedKeys = new List<KeyboardKeys>();

        public KeyboardController()
        {

        }

        public virtual void Update()
        {
            newKeyboard = Keyboard.GetState();

            GetPressedKeys();

        }

        public void UpdateOld()
        {
            oldKeyboard = newKeyboard;

            previousPressedKeys = new List<KeyboardKeys>();
            for (int i = 0; i < pressedKeys.Count; i++)
            {
                previousPressedKeys.Add(pressedKeys[i]);
            }
        }

        public bool GetPress(string KEY)
        {

            for (int i = 0; i < pressedKeys.Count; i++)
            {

                if (pressedKeys[i].key == KEY)
                {
                    return true;
                }

            }


            return false;
        }

        public virtual void GetPressedKeys()
        {
            pressedKeys.Clear();
            for (int i = 0; i < newKeyboard.GetPressedKeys().Length; i++)
            {

                pressedKeys.Add(new KeyboardKeys(newKeyboard.GetPressedKeys()[i].ToString(), 1));

            }
        }
    }
}

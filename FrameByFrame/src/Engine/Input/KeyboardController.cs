using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrameByFrame.src.Engine.Input.Keyboard;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;

namespace FrameByFrame.src.Engine.Input
{
    public class KeyboardController
    {
        private KeyboardState _currentKeyboard, _previousKeyboard;

        public KeyboardController()
        {

        }

        public virtual void Update()
        {
            _previousKeyboard = _currentKeyboard;
            _currentKeyboard = Microsoft.Xna.Framework.Input.Keyboard.GetState();
        }

        public void UpdateOld()
        {
            
        }

        public bool GetPress(string KEY)
        {
            Keys key = GetKeyFromString(KEY);
            return IsKeyHeldDown(key);
        }

        public bool GetPressSingle(string KEY)
        {
            Keys key = GetKeyFromString(KEY);
            return OnKeyPress(key);
        }

        public bool OnKeyPress(Keys key)
        {
            if (_currentKeyboard.IsKeyDown(key) &&
                _previousKeyboard.IsKeyUp(key))
            {
                return true;
            }

            return false;
        }
        public bool IsKeyHeldDown(Keys key)
        {
            return _currentKeyboard.IsKeyDown(key);
        }

        public virtual void GetPressedKeys()
        {
            
        }

        public virtual Microsoft.Xna.Framework.Input.Keys GetKeyFromString(string KEY)
        {
            switch (KEY)
            {
                case "A":
                    break;
                case "B":
                    return Keys.B;
                case "C":
                    break;
                case "M":
                    return Keys.M;
                case "N":
                    return Keys.N;
                case "O":
                    return Keys.O;
                case "P":
                    return Keys.P;
                case "S":
                    return Keys.S;
                case "T":
                    return Keys.T;
                case "W":
                    return Keys.W;
                case "ESC":
                    return Keys.Escape;
                case "DELETE":
                    return Keys.Delete;
                case "BACKSPACE":
                    return Keys.Back;
            }

            return Keys.Sleep;
        }
    }
}

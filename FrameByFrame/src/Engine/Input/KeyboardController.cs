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
                    return Keys.A;
                case "B":
                    return Keys.B;
                case "C":
                    return Keys.C;
                case "D":
                    return Keys.D;
                case "E":
                    return Keys.E;
                case "F":
                    return Keys.F;
                case "G":
                    return Keys.G;
                case "H":
                    return Keys.H;
                case "I":
                    return Keys.I;
                case "J":
                    return Keys.J;
                case "K":
                    return Keys.K;
                case "L":
                    return Keys.L;
                case "M":
                    return Keys.M;
                case "N":
                    return Keys.N;
                case "O":
                    return Keys.O;
                case "P":
                    return Keys.P;
                case "Q":
                    return Keys.Q;
                case "R":
                    return Keys.R;
                case "S":
                    return Keys.S;
                case "T":
                    return Keys.T;
                case "U":
                    return Keys.U;
                case "V":
                    return Keys.V;
                case "W":
                    return Keys.W;
                case "X":
                    return Keys.X;
                case "Y":
                    return Keys.Y;
                case "Z":
                    return Keys.Z;
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

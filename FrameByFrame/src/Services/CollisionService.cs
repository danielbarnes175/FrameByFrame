using FrameByFrame.src.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace FrameByFrame.src.Services
{
    public static class CollisionService
    {
        /**
         * Helper method to determine if two textures collide. Returns true if they collide, otherwise false. Adapted from lecture notes.
         */
        public static bool CheckTexturesCollision(BasicTexture texture1, BasicTexture texture2)
        {
            Rectangle rect1 = new Rectangle((int)texture1.position.X, (int)texture1.position.Y, (int)texture1.dimensions.X, (int)texture1.dimensions.Y);
            Rectangle rect2 = new Rectangle((int)texture2.position.X, (int)texture2.position.Y, (int)texture2.dimensions.X, (int)texture2.dimensions.Y);

            if (rect1.Intersects(rect2))
            {
                Rectangle intersection = Rectangle.Intersect(rect1, rect2);

                for (int x = intersection.X; x < intersection.X + intersection.Width; x++)
                {
                    for (int y = intersection.Y; y < intersection.Y + intersection.Height; y++)
                    {
                        int pixel1 = texture1.colorData[x - (int)texture1.position.X, y - (int)texture1.position.Y].A;
                        int pixel2 = texture2.colorData[x - (int)texture2.position.X, y - (int)texture2.position.Y].A;

                        if (pixel1 != 0 && pixel2 != 0) return true;
                    }
                }
            }
            return false;
        }

        public static bool CheckMouseCollision(BasicTexture texture1)
        {
            Rectangle rect1 = new Rectangle((int)texture1.position.X, (int)texture1.position.Y, (int)texture1.dimensions.X, (int)texture1.dimensions.Y);
            Rectangle mouseRect = new Rectangle((int)GlobalParameters.GlobalMouse.newMousePos.X, (int)GlobalParameters.GlobalMouse.newMousePos.Y, 1, 1);

            if (rect1.Intersects(mouseRect))
            {
                Rectangle intersection = Rectangle.Intersect(rect1, mouseRect);
                Console.WriteLine("Intersecting!");
                for (int x = intersection.X; x < intersection.X + intersection.Width; x++)
                {
                    for (int y = intersection.Y; y < intersection.Y + intersection.Height; y++)
                    {
                        int pixel1 = texture1.colorData[x - (int)texture1.position.X, y - (int)texture1.position.Y].A;

                        if (pixel1 != 0) return true;
                    }
                }
            }
            return false;
        }
    }
}

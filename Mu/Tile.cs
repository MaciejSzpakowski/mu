using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlatRedBall;
using FlatRedBall.Screens;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Input;

namespace Mu
{
    public class Tile
    {
        public Sprite zSprite;

        public Tile(string file)
        {
            zSprite = SpriteManager.AddSprite(file);
        }

        public void Destroy()
        {
            SpriteManager.RemoveSprite(zSprite);
        }
    }
}

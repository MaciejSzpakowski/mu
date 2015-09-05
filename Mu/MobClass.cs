using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Screens;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mu
{
    public enum MobClass
    {
        Spider=1, BudgeDragon
    }

    public partial class Mob : Circle
    {
        private void InitClass()
        {
            switch (Class)
            {
                case MobClass.BudgeDragon:
                    File = "bdragon.achx";
                    Name = "Budge Dragon";
                    break;
            }
        }
    }
}

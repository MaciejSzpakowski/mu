using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Input;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Screens;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mu
{
    public class OptionsMenu : Window
    {
        public OptionsMenu() : base(null, true)
        {
            InitProps(new Vector2(-10, 10), new Vector2(20, 20), new Color(0.3f, 0.3f, 0.3f, 0.75f), "Options", Color.White);

            Window closeButton = new Button(this);
            closeButton.InitProps(Position + new Vector2(1, -1), new Vector2(8, 2), new Color(0.1f, 0.1f, 0.1f, 1), "Back", Color.White);
            closeButton.OnClick = delegate ()
            {
                Destroy();
            };

        }
    }
}

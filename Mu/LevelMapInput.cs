using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall;
using FlatRedBall.Screens;
using FlatRedBall.Math.Geometry;
using System.IO;
using Microsoft.Xna.Framework;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using static FlatRedBall.Input.InputManager;

namespace Mu
{
    public partial class LevelMap : Screen
    {
        private void Input()
        {
            if (Keyboard.KeyPushed(Keys.Enter) && CanStartChat())
            {
                Globals.Chat.StartTyping();
                Keyboard.IgnoreKeyForOneFrame(Keys.Enter);
            }
        }

        private bool CanStartChat() => !Globals.EventManager.HasEvent("typingroutine");
    }
}

using System;
using System.Collections.Generic;

using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Utilities;
using FlatRedBall.Screens;
using Microsoft.Xna.Framework;
using System.Linq;
using IOPath = System.IO.Path;

namespace Mu
{
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);

#if WINDOWS_PHONE || ANDROID || IOS

			// Frame rate is 30 fps by default for Windows Phone,
            // so let's keep that for other phones too
            TargetElapsedTime = TimeSpan.FromTicks(333333);
            graphics.IsFullScreen = true;
#else
            graphics.PreferredBackBufferHeight = 600;
#endif
        }

        private void InitDebug()
        {
            System.Windows.Forms.Form gameWindow = (System.Windows.Forms.Form)System.Windows.Forms.Control.FromHandle(Window.Handle);
            Globals.Console = new Console();
            Globals.Console.Show(gameWindow);
            Globals.PrintDebug = new PrintDebug();
            Globals.PrintDebug.Show(gameWindow);
            Globals.DebugString = GetHashCode().ToString();
            Window.Title = "Mu " + Globals.DebugString;
            Globals.PrintDebug.Text = "PrintDebug " + Globals.DebugString;
            Globals.Console.Text = "Console " + Globals.DebugString;
        }

        private void LoadGlobalAssets()
        {
            Globals.Font = new BitmapFont(IOPath.Combine(Path.Font,"gamefont_small1024_2.png"), 
                IOPath.Combine(Path.Font, "gamefont_small1024.fnt"), FlatRedBallServices.GlobalContentManager);
        }

        protected override void Initialize()
        {
            Globals.Game = this;
            if (Globals.CommandLineArgs.Contains("debug"))
                Globals.DebugMode = true;
            if (Globals.DebugMode)
                InitDebug();

            FlatRedBallServices.InitializeFlatRedBall(this, graphics);
            FlatRedBallServices.IsWindowsCursorVisible = true;
            FlatRedBallServices.GraphicsOptions.TextureFilter = Microsoft.Xna.Framework.Graphics.TextureFilter.Point;
            LoadGlobalAssets();
            ScreenManager.Start(Globals.FirstScreen);

            base.Initialize();
        }


        protected override void Update(GameTime gameTime)
        {
            FlatRedBallServices.Update(gameTime);
            Globals.GameTime = gameTime;
            Globals.EventManager.PreActivity();
            ScreenManager.Activity();
            Globals.EventManager.PostActivity();
            if(Globals.DebugMode)
                Globals.PrintDebug.PrintDebugString();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            FlatRedBallServices.Draw();

            base.Draw(gameTime);
        }
    }
}

using System;
using System.Collections.Generic;

using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Utilities;
using FlatRedBall.Screens;
using Microsoft.Xna.Framework;
using System.Linq;
using IOPath = System.IO.Path;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using static FlatRedBall.Input.InputManager;

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
            graphics.PreferredBackBufferWidth = 800;
#endif
        }

        private void InitDebug()
        {
            System.Windows.Forms.Form gameWindow = (System.Windows.Forms.Form)System.Windows.Forms.Control.FromHandle(Window.Handle);
            Debug.Console = new Console();
            Debug.Console.Show(gameWindow);
            Debug.PrintDebug = new PrintDebug();
            Debug.PrintDebug.Show(gameWindow);
            string unique = Window.Handle.ToString();
            Window.Title = $"Mu {unique}";
            Debug.PrintDebug.Text = $"PrintDebug {unique}";
            Debug.Console.Text = $"Console {unique}";
        }

        private void LoadGlobalAssets()
        {
            Globals.Font = new BitmapFont(IOPath.Combine(Path.Font,"font.png"), 
                IOPath.Combine(Path.Font, "font.fnt"), FlatRedBallServices.GlobalContentManager);
        }

        protected override void Initialize()
        {
            Path.SetRoot(".");
            Path.SetRoot("c:\\bajery\\mu");
            //Path.SetRoot("C:\\Users\\Maciej\\OneDrive\\mu");
            if (Globals.CommandLineArgs.Contains("debug"))
                Debug.DebugMode = true;
            if (Debug.DebugMode)
                InitDebug();

            FlatRedBallServices.InitializeFlatRedBall(this, graphics);
            FlatRedBallServices.IsWindowsCursorVisible = true;
            FlatRedBallServices.GraphicsOptions.TextureFilter = Microsoft.Xna.Framework.Graphics.TextureFilter.Point;
            //FlatRedBallServices.GraphicsOptions.SetFullScreen(800, 600);
            LoadGlobalAssets();
            Ini.LoadIni(Path.Make(Path.Root,"game.ini"));
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
            if (Debug.DebugMode)
            {
                if (Keyboard.KeyDown(Keys.LeftControl) &&
                    Keyboard.KeyPushed(Keys.H))
                {
                    Debug.Console.Visible = !Debug.Console.Visible;
                    Debug.PrintDebug.Visible = !Debug.PrintDebug.Visible;
                }
                Debug.PrintDebug.PrintDebugString();
                Debug.DequeueNewMessages();
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            FlatRedBallServices.Draw();

            base.Draw(gameTime);
        }
    }
}

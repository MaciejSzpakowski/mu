using FlatRedBall.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOPath = System.IO.Path;

namespace Mu
{
    //singeltons and global variables
    public static class Globals
    {
        //variables
        public static string[] CommandLineArgs = null;
        public static bool DebugMode = false;
        public static string DebugString = string.Empty;
        public static Type FirstScreen = typeof(MainMenu);
        public static BitmapFont Font = null;
        public static GameTime GameTime = new GameTime();

        //singletons
        public static Client Client = new Client();
        public static Console Console = null;
        public static EventManager EventManager = new EventManager();
        public static Game1 Game = null;
        public static MuGuiManager GuiManager = new MuGuiManager();
        public static PrintDebug PrintDebug = null;
        public static Server Server = new Server();

        //aliases
        public static void Write(string text)
        {
            if (DebugMode)
                Console.Write(text);
        }

        public static void Debug(object objToPrint, string desc = "")
        {
            if (DebugMode)
                PrintDebug.Debug(objToPrint, desc);
        }
    }

    public static class Path
    {
        public static readonly string Root = "c:\\bajery\\mu";
        public static readonly string Data = IOPath.Combine(Root, "data");
        public static readonly string Texture = Data;
        public static readonly string Font = Data;
    }

    public static class Functions
    {
        public static Color GetColor(this Text text)
        {
            return new Color(text.Red, text.Green, text.Blue, text.Alpha);
        }

        public static void ClipCursorInGameWindow()
        {
            var rect = Globals.Game.Window.ClientBounds;
            System.Drawing.Rectangle client = new System.Drawing.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
            System.Windows.Forms.Cursor.Clip = client;
        }

        public static void UnclipCursor()
        {
            System.Windows.Forms.Cursor.Clip = System.Drawing.Rectangle.Empty;
        }
    }
}

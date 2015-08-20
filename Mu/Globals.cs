using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Animation;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
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
        public static string HeroFile = "";
        public static string Ip = "";
        public static List<Hero> Players = new List<Hero>();
        public static ushort Port = 0;

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

    public static class ZLayer
    {
        public static float Ground = 0;
        public static float Bacground = 0.001f;
        public static float ItemGround = 0.002f;
        public static float Npc = 0.003f;
        public static float Foreground = 0.004f;
        public static float Particle = 0.005f;
        public static float NpcLabel = 0.006f;
        
    }

    public static class Path
    {
        public static string Root;
        public static string Data;
        public static string Texture;
        public static string Font;
        public static string Save;
        public static string Map;

        public static string Make(params string[] paths)
        {
            return IOPath.Combine(paths);
        }
        public static void Init(string root)
        {
            Root = root;
            Data = IOPath.Combine(Root, "data");
            Texture = Data;
            Font = Data;
            Save = IOPath.Combine(Root, "saves");
            Map = Data;
        }
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

        public static float Clamp(this float f, float min, float max)
        {
            if (max < min)
                throw new ArgumentOutOfRangeException("max must be greater than min");
            if (f > max)
                return max;
            else if (f < min)
                return min;
            else
                return f;
        }

        public static int Clamp(this int i, int min, int max)
        {
            if (max < min)
                throw new ArgumentOutOfRangeException("max must be greater than min");
            if (i > max)
                return max;
            else if (i < min)
                return min;
            else
                return i;
        }

        public static float Distance2D(this PositionedObject t, PositionedObject p)
        {
            Vector3 v3 = t.Position - p.Position;
            v3.Z = 0;
            return v3.Length();
        }

        public static void Serialize(string fileName, object obj)
        {
            FileStream fs = new FileStream(fileName, FileMode.Create);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            bf.Serialize(fs, obj);
            fs.Close();
        }

        public static object Deserialize(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            object deserialized = bf.Deserialize(fs);
            fs.Close();
            return deserialized;
        }

        public static Sprite AddSpriteFromAchx(string achxFile)
        {
            return SpriteManager.AddSprite(FlatRedBallServices.Load<AnimationChainList>(achxFile,
                    FlatRedBallServices.GlobalContentManager));
        }

        public static void Resize(this Sprite s, float factor)
        {
            s.ScaleX *= factor;
            s.ScaleY *= factor;
        }

        public static void SetScale(this Sprite s, float x, float y)
        {
            s.ScaleX = x;
            s.ScaleY = y;
        }

        /// <summary>
        /// Get array of bytes and reinterprets it as array of objects
        /// order of objects has to be provided as format string
        /// elements:b - bool, c - char/byte, i - int, f - float, s - string
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public static object[] GetData(byte[] sourceArray, string elements)
        {
            //start at 1 because 0 is header
            int sourceIndex = 1;
            int destinationIndex = 0;
            object[] result = new object[elements.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] == 'b')
                {
                    result[destinationIndex] = sourceArray[sourceIndex] == 0 ? false : true;
                    sourceIndex++;
                    destinationIndex++;
                }
                else if (elements[i] == 'c')
                {
                    result[destinationIndex] = sourceArray[sourceIndex];
                    sourceIndex++;
                    destinationIndex++;
                }
                else if (elements[i] == 'i')
                {
                    result[destinationIndex] = BitConverter.ToInt32(sourceArray, sourceIndex);
                    sourceIndex += 4;
                    destinationIndex++;
                }
                else if (elements[i] == 'f')
                {
                    result[destinationIndex] = BitConverter.ToSingle(sourceArray, sourceIndex);
                    sourceIndex += 4;
                    destinationIndex++;
                }
                else if (elements[i] == 's')
                {
                    int strLen = Convert.ToInt32(sourceArray[sourceIndex]);
                    sourceIndex++;
                    result[destinationIndex] = System.Text.Encoding.ASCII.GetString(sourceArray, sourceIndex, strLen);
                    sourceIndex += strLen;
                    destinationIndex++;
                }
                else
                    throw new ArgumentException("ReadMessage(), unrecognized char");
            }
            return result;
        }
    }

    public static class Rng
    {
        private static Random rnd = new Random();

        /// <summary>
        /// Returns foat between 0.0 and 1.0
        /// </summary>
        /// <returns></returns>
        public static float NextFloat()
        {
            return (float)rnd.NextDouble();
        }

        /// <summary>
        /// Returns float between min and max
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static float NextFloat(float min, float max)
        {
            if (max <= min)
                throw new ArgumentOutOfRangeException("max must be greater than min");
            double d = rnd.NextDouble() * (max - min) + min;
            return (float)d;
        }

        /// <summary>
        /// Returns int between min and max
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int NextInt(int min, int max)
        {
            if (max <= min)
                throw new ArgumentOutOfRangeException("max must be greater than min");
            return rnd.Next(min, max + 1);
        }

        public static void TextRng()
        {
            for (int k = 0; k < 100000; k++)
            {
                float f = NextFloat(-11, 15);
                int i = NextInt(-11, 15);
                if (f < -11 || f > 15 || i < -11 || i > 15)
                    throw new ArgumentOutOfRangeException("Rng doesn't work");
            }
        }
    }
}

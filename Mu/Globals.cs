using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Animation;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using IOPath = System.IO.Path;

namespace Mu
{
    //singeltons and global variables
    public static class Globals
    {
        //variables
        public static string[] CommandLineArgs = null;        
        public static Type FirstScreen = typeof(MainMenu);
        public static BitmapFont Font = null;
        public static GameTime GameTime = new GameTime();
        public static string HeroFile = "";
        public static string Ip = "";
        public static List<Hero> Players = new List<Hero>();
        public static ushort Port = 0;

        //singletons
        public static Chat Chat = null;
        public static Client Client = null;        
        public static EventManager EventManager = new EventManager();
        public static MuGuiManager GuiManager = new MuGuiManager();        
        public static Server Server = null;        
    }

    public static class Debug
    {
        private static int UniqueInt = 1;
        public static bool DebugMode = false;
        public static PrintDebug PrintDebug = null;
        public static Console Console = null;
        //Debug
        private static Queue<string> newMessages = new Queue<string>();
        private static Mutex msgQueueMutex = new Mutex();
        public static void Write(string text)
        {
            if (DebugMode)
            {
                msgQueueMutex.WaitOne();
                newMessages.Enqueue(text);
                msgQueueMutex.ReleaseMutex();
            }
        }

        public static void Print(object objToPrint, string desc = "")
        {
            if (DebugMode)
                PrintDebug.Print(objToPrint, desc);
        }

        internal static void DequeueNewMessages()
        {
            msgQueueMutex.WaitOne();
            while (newMessages.Count > 0)
            {
                string str = newMessages.Dequeue();
                Console.Write(str);
            }
            msgQueueMutex.ReleaseMutex();            
        }

        public static int GetUnique()
        {
            int res = UniqueInt;
            UniqueInt++;
            return res;
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
        public static string Save;

        public static string Hero;
        public static string Mob;
        public static string Font;       
        public static string Map;
        public static string Misc;
        public static string Item;

        public static string Make(params string[] paths) => IOPath.Combine(paths);

        public static void SetRoot(string root)
        {
            Root = root;
            Data = IOPath.Combine(Root, "data");
            Save = IOPath.Combine(Root, "save");
            Hero = IOPath.Combine(Data, "hero");
            Mob = IOPath.Combine(Data, "mobs");
            Font = IOPath.Combine(Data, "fonts");
            Map = IOPath.Combine(Data, "maps");
            Misc = IOPath.Combine(Data, "misc");
            Item = IOPath.Combine(Data, "items");
            Map = IOPath.Combine(Data, "maps");
        }
    }

    public static class Functions
    {
        public static Color GetColor(this Text text) => new Color(text.Red, text.Green, text.Blue, text.Alpha);

        public static void SetColor(this Text t, Color c)
        {
            Vector4 v = new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
            t.SetColor(v.X, v.Y, v.Z);
            t.Alpha = v.W;
        }

        public static void ClipCursorInGameWindow()
        {
            var rect = FlatRedBallServices.Game.Window.ClientBounds;
            System.Drawing.Rectangle client = new System.Drawing.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
            System.Windows.Forms.Cursor.Clip = client;
        }

        public static void UnclipCursor()
        {
            System.Windows.Forms.Cursor.Clip = System.Drawing.Rectangle.Empty;
        }

        public static T Clamp<T>(this T f, T min, T max) where T : IComparable
        {
            if (min.CompareTo(max) > 0)
                throw new ArgumentOutOfRangeException("max must be greater than min");
            if (f.CompareTo(max) > 0)
                return max;
            else if (f.CompareTo(min) < 0)
                return min;
            else
                return f;
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
        /// elements:b - bool, c - char/byte, i - int, f - float, s - string
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public static List<object> GetData(byte[] sourceArray)
        {
            //start at 1 because 0 is header
            int sourceIndex = 1;
            List<object> result = new List<object>();
            while(sourceIndex < sourceArray.Length)
            {
                byte currentByte = sourceArray[sourceIndex++];
                //bool
                if (currentByte == 'b')
                {
                    result.Add(sourceArray[sourceIndex++] == 0 ? false : true);
                }
                //char/byte
                else if (currentByte == 'c')
                {
                    result.Add(sourceArray[sourceIndex++]);
                }
                //int
                else if (currentByte == 'i')
                {
                    result.Add(BitConverter.ToInt32(sourceArray, sourceIndex));
                    sourceIndex += 4;
                }
                //float
                else if (currentByte == 'f')
                {
                    result.Add(BitConverter.ToSingle(sourceArray, sourceIndex));
                    sourceIndex += 4;
                }
                //string
                else if (currentByte == 's')
                {
                    int strLen = Convert.ToInt32(sourceArray[sourceIndex++]);
                    result.Add(Encoding.ASCII.GetString(sourceArray, sourceIndex, strLen));
                    sourceIndex += strLen;
                }
                else
                    throw new ArgumentException("unrecognized format");
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
        public static float NextFloat() => (float)rnd.NextDouble();

        /// <summary>
        /// Returns float between greate or equal min and less than max
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static float NextFloat(float min, float max)
        {
            if (max < min)
                throw new ArgumentOutOfRangeException("max must be greater or equal min");
            double d = rnd.NextDouble() * (max - min) + min;
            return (float)d;
        }

        /// <summary>
        /// Returns int between min and max both inclusive
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int NextInt(int min, int max)
        {
            if (max < min)
                throw new ArgumentOutOfRangeException("max must be greater or equal min");
            return rnd.Next(min, max + 1);
        }

        /// <summary>
        /// Tries event based on chance
        /// </summary>
        /// <param name="chance"></param>
        /// <returns></returns>
        public static bool Try(double chance) => rnd.NextDouble() < chance;

        public static void TestRng()
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

    public static class Ini
    {
        public static string Server = "";
        public static string Port = "";

        public static void LoadIni(string file)
        {
            try
            {
                string[] ini = File.ReadAllLines(file);
                foreach (string s in ini.Where(str => str.Contains('=') && str[0] != ';'))
                    ProcessLine(s);
            }
            catch (Exception e)
            {
                Debug.Write("Ini has errors: "+e.Message);
            }
        }

        private static void ProcessLine(string s)
        {
            string[] keyValue = s.Split('=');
            string key = keyValue[0].Replace(" ", "").ToUpper();
            string value = keyValue[1].Replace(" ", "");

            switch (key)
            {
                case ("SERVER"):
                    Server = value;
                    break;
                case ("PORT"):
                    Port = value;
                    break;
                default:
                    break;
            }

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlatRedBall;
using FlatRedBall.Screens;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Input;
using System.IO;
using Microsoft.Xna.Framework;

namespace Mu
{
    public class Map
    {
        List<Tile> zTiles;

        public Map(string file)
        {
            zTiles = new List<Tile>();

            LoadFromFile(file);
        }

        private void LoadFromFile(string file)
        {
            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                string[] tokens = line.Split(' ');
                if (tokens.Length == 0)
                    continue;
                if (tokens[0] == "tile")
                    AddTile(tokens);
            }
        }

        private void AddTile(string[] tokens)
        {
            Tile t = new Tile(Path.Make(Path.Texture, tokens[1]));
            Vector3 pos = StringToVector3(tokens[3]);
            Vector3 scale = StringToVector3(tokens[5]);
            t.zSprite.Position = pos;
            t.zSprite.SetScale(scale.X, scale.Y);
        }

        private Vector3 StringToVector3(string v)
        {
            Vector3 result = Vector3.Zero;
            string[] e = v.Split(',', '(', ')');
            if (float.TryParse(e[0], out result.X) && float.TryParse(e[1], out result.Y) && float.TryParse(e[2], out result.Z) == false)
                throw new ArgumentException("Invalid format");
            return result;
        }

        public void Destroy()
        {
            foreach (Tile t in zTiles)
                t.Destroy();
        }
    }

    public class LevelMap : Screen
    {
        public LevelMap()
            : base("LevelMap")
        {
        }

        public override void Initialize(bool addToManagers)
        {
            //i think this should be first
            base.Initialize(addToManagers);
            TestInit();
        }

        public override void Activity(bool firstTimeCalled)
        {
            //test
            TestActivity();
            //server and client
            Globals.Server.Activity();
            Globals.Client.Activity();

            //i think this should be at the end
            base.Activity(firstTimeCalled);
        }

        public override void Destroy()
        {
            base.Destroy();
        }

        public void TestInit()
        {
        }

        public void TestActivity()
        {
        }
    }

}

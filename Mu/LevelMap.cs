using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlatRedBall;
using FlatRedBall.Screens;
using FlatRedBall.Math.Geometry;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using static FlatRedBall.Input.InputManager;
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

        public static string MobmapToString(MobMap map)
        {
            switch (map)
            {
                case MobMap.Lorencia:
                    return "lorencia.map";
                case MobMap.Noria:
                    return "noria.map";
                case MobMap.Dungeon:
                    return "dungeon.map";
                case MobMap.Devias:
                    return "devias.map";
                case MobMap.LostTower:
                    return "losttower.map";
                default:
                    throw new NotImplementedException("This map is not implemented");
            }
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
            Tile t = new Tile(Path.Make(Path.Map, tokens[1]));
            Vector3 pos = StringToVector3(tokens[3]);
            Vector3 scale = StringToVector3(tokens[5]);
            t.zSprite.Position = pos;
            t.zSprite.SetScale(scale.X, scale.Y);
            zTiles.Add(t);
        }

        private Vector3 StringToVector3(string v)
        {
            Vector3 result = Vector3.Zero;
            v = v.Replace("(", "");
            v = v.Replace(")", "");
            string[] e = v.Split(',');
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

    public partial class LevelMap : Screen
    {
        public LevelMap()
            : base("LevelMap")
        {
        }

        Map Map;
        List<Event> Events;

        public override void Initialize(bool addToManagers)
        {
            //i think this should be first
            base.Initialize(addToManagers);
            TestInit();
            InitHero();
            InitMap();
            InitEvents();
            Globals.Client.SendReady();
            Globals.Players[0].StartUpdatingPos();
            Globals.Chat = new Chat();
        }

        public void Exit()
        {            
            ScreenManager.CurrentScreen.MoveToScreen(typeof(MainMenu));
        }

        private void InitEvents()
        {
            Events = new List<Event>();
            // escape event
            Event e1 = Globals.EventManager.AddEvent(delegate ()
            {
                if (Keyboard.KeyPushed(Keys.Escape))
                    new MessageBox("Exit ?", MessageBoxType.YesNo, "levelmapexit");
                if (Globals.GuiManager.CompareMBresult(MessageBoxReturn.YES, "levelmapexit"))
                    Exit();
                return 1;
            }, "escapemap");
            Events.Add(e1);
            // disconnected
            e1 = Globals.EventManager.AddEvent(delegate ()
            {
                if (Globals.GuiManager.CompareMBresult(MessageBoxReturn.OK, "disconnected"))
                    Exit();
                return 1;
            }, "disconnected");
            Events.Add(e1);
        }

        private void InitMap()
        {
            Map = new Map(Path.Make(Path.Map, Map.MobmapToString(Globals.Players[0].Map)));
        }

        private void InitHero()
        {
            SaveHero sh = (SaveHero)Functions.Deserialize(Path.Make(Path.Save, Globals.HeroFile));
            Hero h = sh.ToHero();
            Globals.Players.Add(h);
        }

        public override void Activity(bool firstTimeCalled)
        {
            //test
            TestActivity();
            //gui
            Globals.GuiManager.Activity();
            Input();
            //server and client
            if(Globals.Server != null)
                Globals.Server.Activity();
            Globals.Client.Activity();
            //players
            for (int i = 0; i < Globals.Players.Count; i++)
                Globals.Players[i].Activity(i == 0);
            CameraLookAtPlayer();

            //i think this should be at the end
            base.Activity(firstTimeCalled);
        }

        private void CameraLookAtPlayer()
        {
            if (Globals.Players.Count > 0)
            {
                SpriteManager.Camera.Position.X = Globals.Players[0].X;
                SpriteManager.Camera.Position.Y = Globals.Players[0].Y;
            }
        }

        public override void Destroy()
        {
            Globals.Client.Disconnect(false);
            Globals.Client.Destroy();
            if (Globals.Server?.GetState() == ServerState.Running)
                Globals.Server.RemoveMobs();
            foreach (Event e in Events)
                Globals.EventManager.RemoveEvent(e);
            foreach (Hero h in Globals.Players)
                h.Destroy();
            Globals.Players.Clear();
            Globals.GuiManager.Clear();
            Globals.Chat = null;
            Map.Destroy();
            base.Destroy();
        }

        public void TestInit()
        {
        }

        public void TestActivity()
        {
            Debug.Print(Mouse.WorldXAt(0).ToString() + " " 
                + Mouse.WorldXAt(0).ToString());
        }
    }

}

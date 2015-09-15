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
        private List<Tile> Tiles;

        public Map(string file)
        {
            Tiles = new List<Tile>();

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
                switch (tokens[0])
                {
                    case "sprite":
                        AddSprite(tokens);
                        break;
                    case "trigger":
                        AddTrigger(tokens);
                        break;
                    case "empty":
                        AddEmpty(tokens);
                        break;
                }
            }
        }

        private void AddEmpty(string[] tokens)
        {
            Vector3 pos = StringToVector3(tokens[2]);
            var t = new EmptyTile(pos, tokens[4]);
            Tiles.Add(t);
        }

        private void AddTrigger(string[] tokens)
        {
            Vector3 pos = StringToVector3(tokens[2]);
            float radius = 0;
            float.TryParse(tokens[4], out radius);
            var t = new TriggerTile(pos, radius, tokens[6]);
            Tiles.Add(t);
        }

        private void AddSprite(string[] tokens)
        {
            Vector3 pos = StringToVector3(tokens[3]);
            Vector3 scale = StringToVector3(tokens[5]);
            var t = new SpriteTile(Path.Make(Path.Map, tokens[1]), pos, scale.X, scale.Y);
            Tiles.Add(t);
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

        /// <summary>
        /// Warp player to Empty tile by name
        /// </summary>
        /// <param name="name"></param>
        public void GotoEmpty(string name)
        {
            Globals.Players[0].Position = Tiles.First(t => t.GetName() == name).GetPosition();
            Globals.Players[0].Position.Z = ZLayer.Npc;
        }

        public void Destroy()
        {
            foreach (Tile t in Tiles)
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

        public void ChangeMap(string dstMap, string entryPoint)
        {
            //change map first
            Globals.Players[0].Map = dstMap.StringToMobmap();

            //stop sending staff to server and unload map
            Globals.Players[0].StopUpdatingPos();
            Globals.Client.SendQuit();
            Globals.Client.DestroyMobs();
            Map.Destroy();
            while (Globals.Players.Count > 1)
                Globals.Players.Last.Destroy();

            //load map staff            
            InitMap();
            Map.GotoEmpty(entryPoint);
            Globals.Client.SendReady();
            Globals.Players[0].StartUpdatingPos();
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
            Map = new Map(Path.Make(Path.Map, Globals.Players[0].Map.MobmapToString()));
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
            Globals.Server?.Activity();
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
            while (Globals.Players.Count > 0)
                Globals.Players.Last.Destroy();
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

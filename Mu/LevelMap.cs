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

    public class LevelMap : Screen
    {
        public LevelMap()
            : base("LevelMap")
        {
        }

        Map zMap;
        List<Event> zEvents;

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
            Globals.Client.Disconnect(false);
            Destroy();
            ScreenManager.CurrentScreen.MoveToScreen(typeof(MainMenu));
        }

        private void InitEvents()
        {
            zEvents = new List<Event>();
            // escape event
            Event e1 = Globals.EventManager.AddEvent(delegate ()
            {
                if (InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Escape))
                    new MessageBox("Exit ?", MessageBoxType.YesNo, "levelmapexit");
                if (Globals.GuiManager.GetMbResult(MessageBoxReturn.YES, "levelmapexit"))
                    Exit();
                return 1;
            }, "escapemap");
            zEvents.Add(e1);
            // disconnected
            e1 = Globals.EventManager.AddEvent(delegate ()
            {
                if (Globals.GuiManager.GetMbResult(MessageBoxReturn.OK, "disconnected"))
                    Exit();
                return 1;
            }, "disconnected");
            zEvents.Add(e1);
        }

        private void InitMap()
        {
            zMap = new Map(Path.Make(Path.Map,Globals.Players[0].Map));
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
            foreach (Event e in zEvents)
                Globals.EventManager.RemoveEvent(e);
            foreach (Hero h in Globals.Players)
                h.Destroy();
            Globals.Players.Clear();
            Globals.GuiManager.Clear();
            Globals.Chat = null;
            zMap.Destroy();
            base.Destroy();
        }

        public void TestInit()
        {
        }

        public void TestActivity()
        {
            Debug.Print(InputManager.Mouse.WorldXAt(0).ToString() + " " 
                + InputManager.Mouse.WorldXAt(0).ToString());
        }
    }

}

using FlatRedBall.Graphics;
using FlatRedBall.Input;
using FlatRedBall.Screens;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mu
{
    public class MainMenu : Screen
    {
        public MainMenu()
            : base("MainMenu")
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
            Globals.GuiManager.Activity();

            //test
            TestActivity();

            //i think this should be at the end
            base.Activity(firstTimeCalled);
        }

        public override void Destroy()
        {
            Globals.GuiManager.Clear();
            base.Destroy();
        }

        public void TestInit()
        {
            Window w = new Window();
            w.OnClick = delegate () { Globals.Write(w.Position.ToString()); };
            w.Color = Color.Red;
            w.Transparency = 0.75f;
            w.Size = new Vector2(10, 10);
            w.Text = "Window1";
            Globals.GuiManager.AddWindow(w);
            Window w1 = new Window();
            w1.OnClick = delegate () { Globals.Write(w1.Position.ToString()); };
            w1.Color = Color.Green;
            w1.Transparency = 0.75f;
            w1.Size = new Vector2(8, 8);
            w1.Text = "Window2";
            Globals.GuiManager.AddWindow(w1);
            Window w2 = new Window();
            w2.OnClick = delegate () { Globals.Write(w2.Position.ToString()); };
            w2.Color = Color.Blue;
            w2.Transparency = 0.75f;
            w2.Size = new Vector2(6, 10);
            w2.Text = "Window3";
            Globals.GuiManager.AddWindow(w2);

            Window child = new Window(w);
            child.OnClick = delegate () { child.Size -= new Vector2(1, 1); };
            child.Position = new Vector2(1, -1);
            child.Size = new Vector2(8, 8);
            child.Text = "Child";
            child.TextColor = Color.Black;
        }

        public void TestActivity()
        {
            var cur = new Vector2(InputManager.Mouse.WorldXAt(0), InputManager.Mouse.WorldYAt(0));
            Globals.Debug(cur.X, "x");
            Globals.Debug(cur.Y, "y");
        }
    }
}

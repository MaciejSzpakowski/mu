using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlatRedBall;
using FlatRedBall.Screens;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Input;
using Microsoft.Xna.Framework;

namespace Mu
{
    public abstract class Tile
    {
        protected string Name;

        public Tile()
        {
            Name = string.Empty;
        }
        public abstract void Destroy();
        public abstract Vector3 GetPosition();
        public string GetName() => Name;
    }

    public class SpriteTile : Tile
    {
        private Sprite Sprite;

        public SpriteTile(string file, Vector3 pos, float scalex, float scaley)
        {
            Sprite = SpriteManager.AddSprite(file);
            Sprite.Position = pos;
            Sprite.SetScale(scalex, scaley);
        }

        public override Vector3 GetPosition() => Sprite.Position;

        public override void Destroy()
        {
            SpriteManager.RemoveSprite(Sprite);
        }
    }

    public class TriggerTile : Tile
    {
        private Circle Collider;
        private bool Collect;
        private VoidFunction Func;
        private static int Id = 0;

        public TriggerTile(Vector3 pos, float radius, string name)
        {
            Collect = false;
            Name = name;
            Collider = ShapeManager.AddCircle();
            Collider.Position = pos;
            Collider.Radius = radius;
            Globals.EventManager.AddEvent(Trigger, $"trigger{Id}");
            Id++;
            InitializeEvent();
        }

        private void InitializeEvent()
        {
            if (Name.Contains("warp"))
                InitializeWarp();
        }

        private void InitializeWarp()
        {
            string mapTo = Name.Split('-')[1] + ".map";
            string entryPoint = "from" + Globals.Players[0].Map.MobmapToString().Split('.')[0];
            Func = delegate ()
            {
                LevelMap map = (LevelMap)ScreenManager.CurrentScreen;
                map.ChangeMap(mapTo, entryPoint);
            };
        }

        private int Trigger()
        {
            if (Collect)
                return 0;
            if (Collider.CollideAgainst(Globals.Players[0].GetCollider))
                Func();
            return 1;
        }

        public override Vector3 GetPosition() => Collider.Position;

        public override void Destroy()
        {
            Collect = true;
            ShapeManager.Remove(Collider);
        }
    }

    public class EmptyTile : Tile
    {
        private Vector3 Position;

        public EmptyTile(Vector3 pos, string name)
        {
            Name = name;
            Position = pos;
        }

        public override Vector3 GetPosition() => Position;

        public override void Destroy()
        {
        }
    }
}

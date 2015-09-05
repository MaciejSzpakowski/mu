using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Screens;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using static FlatRedBall.Input.InputManager;
using static FlatRedBall.Input.Mouse;

namespace Mu
{
    public enum HeroClass { Elf=0, Knight=1, Wizard=2, Invalid=9 };

    public struct HeroStats
    {

    }

    public class Hero : PositionedObject
    {
        private Circle Collider;
        private Sprite Sprite;        
        private Text Label;
        private Sprite HealthBar;
        private HeroStats Stats;
        private float WalkingSpeed;
        public int Netid;
        private Vector3 zLastPosition;
        public bool AcceptArrows;
        public Vector3 Target; //used by other players
        private bool Collect;

        //serializable
        public HeroClass Class;
        public float ExperienceBoost;
        public TimeSpan Online; //how much time hero has been played
        public MobMap Map; //what map is it on
        //stats
        public long Gold;
        public long Level;
        public float Str;
        public float Agi;
        public float Ene;
        public float Vit;
        public long Exp;
        public long ExpPrevious;
        public long ExpNext;
        public long StatPoints;

        public Hero(string name, HeroClass heroClass)
        {
            Collect = false;
            Online = TimeSpan.Zero;
            Target = Position;
            AcceptArrows = true;
            zLastPosition = Position;
            Netid = 0;
            Name = name;
            Class = heroClass;
            SpriteManager.AddPositionedObject(this);
            Collider = ShapeManager.AddCircle();
            Collider.AttachTo(this, false);
            Collider.Visible = false;
            Sprite = LoadSprite();
            Sprite.AttachTo(this, false);
            Sprite.Resize(4);
            Sprite.AnimationSpeed = 0.1f;
            Position.Z = ZLayer.Npc;
            Label = TextManager.AddText(name, Globals.Font);
            Label.AttachTo(this, false);
            Label.HorizontalAlignment = HorizontalAlignment.Center;
            Label.RelativePosition = new Vector3(0, 4, ZLayer.NpcLabel - Position.Z);

            WalkingSpeed = 10;

            InitStats();
        }

        private Sprite LoadSprite()
        {
            if (Class == HeroClass.Elf)
                return Functions.AddSpriteFromAchx(Path.Make(Path.Hero, "elf.achx"));
            else if (Class == HeroClass.Knight)
                return Functions.AddSpriteFromAchx(Path.Make(Path.Hero, "knight.achx"));
            else if (Class == HeroClass.Wizard)
                return Functions.AddSpriteFromAchx(Path.Make(Path.Hero, "wizard.achx"));
            return null;
        }

        private void InitStats()
        {
            ExperienceBoost = 0;
            Online = TimeSpan.Zero;
            Gold = 0;
            Level = 1;
            Exp = 0;
            ExpPrevious = 0;
            ExpNext = 100;
            StatPoints = 0;
            Map = MobMap.Lorencia;
            if (Class == HeroClass.Elf)
            {
                //Map = "noria.map";
                Str = 22;
                Agi = 25;
                Vit = 20;
                Ene = 15;
            }
            else if (Class == HeroClass.Knight)
            {
                Str = 28;
                Agi = 20;
                Vit = 25;
                Ene = 10;
            }
            else if (Class == HeroClass.Wizard)
            {
                Str = 18;
                Agi = 18;
                Vit = 15;
                Ene = 30;
            }
        }

        public void Input()
        {
            if(AcceptArrows)
                InputMovement();
        }

        private void InputMovement()
        {
            //8 directions
            bool left = Keyboard.KeyDown(Keys.Left);
            bool right = Keyboard.KeyDown(Keys.Right);
            bool up = Keyboard.KeyDown(Keys.Up);
            bool down = Keyboard.KeyDown(Keys.Down);
            if (left && up)
                Velocity = new Vector3(-WalkingSpeed * 0.707f, WalkingSpeed * 0.707f, 0);
            else if (right && up)
                Velocity = new Vector3(WalkingSpeed * 0.707f, WalkingSpeed * 0.707f, 0);
            else if (right && down)
                Velocity = new Vector3(WalkingSpeed * 0.707f, -WalkingSpeed * 0.707f, 0);
            else if (left && down)
                Velocity = new Vector3(-WalkingSpeed * 0.707f, -WalkingSpeed * 0.707f, 0);
            else if (left)
                Velocity = new Vector3(-WalkingSpeed, 0, 0);
            else if (up)
                Velocity = new Vector3(0, WalkingSpeed, 0);
            else if (right)
                Velocity = new Vector3(WalkingSpeed, 0, 0);
            else if (down)
                Velocity = new Vector3(0, -WalkingSpeed, 0);
        }

        public void Activity(bool hero)
        {
            Velocity = Vector3.Zero;
            if (hero)
            {
                Input();
            }
            else
            {
                GotoTarget();
            }
            AnimationControl();
        }

        private void GotoTarget()
        {
            float delta = 0;
            Vector3.Distance(ref Target, ref Position, out delta);
            if (delta > 0.1f)
            {
                Velocity = Target - Position;
                Velocity.Normalize();
                Velocity *= WalkingSpeed;
            }
        }

        private bool FlipHorizontal = false;
        public void AnimationControl()
        {
            if ((Velocity != Vector3.Zero) && Sprite.CurrentChainName != "Walk")
            {
                float walkingAnimationSpeed = 0.33f;
                Sprite.AnimationSpeed = walkingAnimationSpeed;
                Sprite.CurrentChainName = "Walk";
            }
            else if (Velocity == Vector3.Zero && Sprite.CurrentChainName != "Idle")
            {
                Sprite.CurrentChainName = "Idle";
            }
            if (Velocity.X < 0 && Sprite.FlipHorizontal)
                FlipHorizontal = false;
            else if (Velocity.X > 0 && !Sprite.FlipHorizontal)
                FlipHorizontal = true;
            Sprite.FlipHorizontal = FlipHorizontal;
        }

        public void StartUpdatingPos()
        {
            Globals.EventManager.AddEvent(UpdatePos, "updatepos", false, 0, 0, 0.1f);
        }

        private int UpdatePos()
        {
            if (Collect)
                return 0;
            if (zLastPosition != Position)
            {
                Globals.Client.SendPos(Position.X, Position.Y);
                zLastPosition = Position;
            }
            return 1;
        }

        public SaveHero ToSavehero()
        {
            SaveHero shero = new SaveHero();
            shero.Class = Class;
            shero.Name = Name;
            shero.Str = Str;
            shero.Vit = Vit;
            shero.Agi = Agi;
            shero.Ene = Ene;
            shero.Level = Level;
            shero.Exp = Exp;
            shero.ExpNext = ExpNext;
            shero.ExpPrevious = ExpPrevious;
            shero.StatPoints = StatPoints;
            shero.Gold = Gold;
            shero.Map = Map;
            shero.Online = Online;
            shero.ExperienceBoost = ExperienceBoost;

            return shero;
        }

        public void Destroy()
        {
            Collect = true;
            ShapeManager.Remove(Collider);
            SpriteManager.RemoveSprite(Sprite);
            SpriteManager.RemoveSprite(HealthBar);
            TextManager.RemoveText(Label);
            SpriteManager.RemovePositionedObject(this);
        }
    }

    [Serializable]
    public class SaveHero
    {
        public MobMap Map;
        public HeroClass Class;
        public string Name;
        public float Str;
        public float Vit;
        public float Agi;
        public float Ene;
        public long Level;
        public long Exp;
        public long ExpNext;
        public long ExpPrevious;
        public long StatPoints;
        public long Gold;
        public TimeSpan Online;
        public float ExperienceBoost;

        public Hero ToHero()
        {
            Hero hero = new Hero(Name, Class);
            hero.Gold = Gold;
            hero.Vit = Vit;
            hero.Str = Str;
            hero.Agi = Agi;
            hero.Ene = Ene;
            hero.Level = Level;
            hero.Exp = Exp;
            hero.ExpNext = ExpNext;
            hero.ExpPrevious = ExpPrevious;
            hero.StatPoints = StatPoints;
            hero.Map = Map;
            hero.Online = Online;
            hero.ExperienceBoost = ExperienceBoost;
            hero.ExperienceBoost = hero.ExperienceBoost.Clamp(1, 2);
            return hero;
        }
    }
}

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

namespace Mu
{
    public enum MobState
    {
        Roaming, Chasing, Atacking, Killed
    }

    public enum MobMap
    {
        Lorencia = 1, Noria, Dungeon, Devias, LostTower
    }

    public partial class Mob : Circle
    {
        private Sprite Sprite = null;
        private string File;
        private Text Label = null;
        private MobState State;
        public int Netid;
        private static int NextNetid = 0;
        public Vector3 Target;
        private float RunSpeed;
        private float RoamSpeed;
        private float MovingSpeed;
        public MobClass Class;
        public bool Removed;
        private Vector3 zLastTarget;
        public MobMap Map;

        private Mob()
        {
            RunSpeed = 7;
            RoamSpeed = 5;
            MovingSpeed = RoamSpeed;
            Removed = false;
            Netid = 0;
            State = MobState.Roaming;
            ShapeManager.AddCircle(this);
        }

        public static Mob MobServer(MobClass mobclass, MobMap map)
        {
            Mob m = new Mob();
            m.Map = map;
            m.Netid = NextNetid++;
            m.Class = mobclass;
            m.StartRoaming();
            m.StartUpdatingPos();
            return m;
        }

        public static Mob MobClient(MobClass mobclass)
        {
            Mob m = new Mob();
            m.Class = mobclass;
            //m.Visible = false;
            m.Color = Color.Red;
            m.InitClass();
            m.InitSprite(m.File);
            m.InitLabel();
            return m;
        }

        public void ServerActivity()
        {
            //restore removed mobs
            if (Removed)
            {
                ShapeManager.AddCircle(this);
                Removed = false;
            }
            CommonActivity();
        }

        public void ClientActivity()
        {
            CommonActivity();
        }

        public void SetName(string name, Color color)
        {
            Name = name;
            Label.DisplayText = name;
            Label.SetColor(color);
        }

        private void CommonActivity()
        {
            GotoTarget();
            switch (State)
            {
                case MobState.Roaming:
                    MovingSpeed = RoamSpeed;
                    break;
                case MobState.Chasing:
                    MovingSpeed = RunSpeed;
                    break;
                case MobState.Atacking:
                    break;
                case MobState.Killed:
                    break;
                default:
                    throw new ArgumentException("How did you get here ?");
            }
        }

        private void InitSprite(string file)
        {
            Sprite = Functions.AddSpriteFromAchx(Path.Make(Path.Mob, file));
            Sprite.CurrentChainName = "Idle";
            Sprite.Resize(4);
            Sprite.IgnoresParentVisibility = true;
            Sprite.AttachTo(this, false);
        }

        private void InitLabel()
        {
            Label = TextManager.AddText(Name, Globals.Font);
            Label.AttachTo(this, false);
            Label.HorizontalAlignment = HorizontalAlignment.Center;
            Label.RelativePosition = new Vector3(0, 4, ZLayer.NpcLabel - Position.Z);
        }         

        /// <summary>
        /// This is to remove mob fom manager so server owning client can jump between level and main menu
        /// </summary>
        public void Remove()
        {
            Velocity = Vector3.Zero;
            ShapeManager.RemoveOneWay(this);
            Removed = true;
        }

        public void StartUpdatingPos()
        {
            Globals.EventManager.AddEvent(UpdateTarget, $"updatepos{Netid}", false, 0, 0, 0.3f);
        }

        private int UpdateTarget()
        {
            if (State == MobState.Killed)
                return 0;
            if (zLastTarget != Target)
            {
                Globals.Server.SendMobTarget(this);
                zLastTarget = Target;
            }
            return 1;
        }

        private void GotoTarget()
        {            
            if (Vector3.Distance(Position, Target) > 0.1f)
            {
                Velocity = Vector3.Normalize(Target - Position) * MovingSpeed;
            }
            else
                Velocity = Vector3.Zero;
        }

        private void StartRoaming()
        {
            int nextPulse = Rng.NextInt(2, 4);
            Globals.EventManager.AddEvent(Roam, $"mobmovement{Netid}", false, nextPulse);
        }

        private int Roam()
        {
            if (State == MobState.Roaming)
            {
                Target = Position + new Vector3(Rng.NextFloat(-4, 4), Rng.NextFloat(-4, 4), 0);
                StartRoaming();
            }
            return 0;
        }

        public void Destroy()
        {
            State = MobState.Killed;
            if(Sprite != null)
                SpriteManager.RemoveSprite(Sprite);
            if(Label != null)
                TextManager.RemoveText(Label);
            ShapeManager.Remove(this);
        }
    }
}

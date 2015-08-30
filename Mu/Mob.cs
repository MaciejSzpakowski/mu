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
        Roaming = 0, Chasing = 1, Atacking = 2
    }

    public partial class Mob : Circle
    {
        private Sprite Sprite = null;
        private Text Label = null;
        private MobState State;
        public int Netid;
        private static int NextNetid = 0;
        private Vector3 RoamTarget;
        private float RoamingSpeed;
        public MobClass Class;
        public bool Removed;

        private Mob()
        {
            RoamingSpeed = 5;
            Removed = false;
            Netid = 0;
            State = MobState.Roaming;
            ShapeManager.AddCircle(this);
        }

        public static Mob MobServer(MobClass mobclass)
        {
            Mob m = new Mob();
            m.Netid = NextNetid++;
            m.Class = mobclass;
            m.StartRoaming();
            return m;
        }

        public static Mob MobClient(MobClass mobclass)
        {
            Mob m = new Mob();
            m.Class = mobclass;
            //m.Visible = false;
            m.Color = Color.Red;
            m.InitClass();
            return m;
        }

        public void ServerActivity()
        {
            if (Removed)
            {
                ShapeManager.AddCircle(this);
                Removed = false;
            }
            switch (State)
            {
                case MobState.Roaming:
                    GotoRoamTarget();
                    break;
                case MobState.Chasing:
                    break;
                case MobState.Atacking:
                    break;
                default:
                    throw new ArgumentException("How did you get here ?");
            }
        }

        public void ClientActivity()
        {
        }

        public void Remove()
        {
            ShapeManager.RemoveOneWay(this);
            Removed = true;
        }

        private void GotoRoamTarget()
        {
            if (Vector3.Distance(Position, RoamTarget) > 0.1f)
            {
                if (Velocity == Vector3.Zero)
                    Velocity = Vector3.Normalize(RoamTarget - Position) * RoamingSpeed;
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
                RoamTarget = Position + new Vector3(Rng.NextFloat(-4, 4), Rng.NextFloat(-4, 4), 0);
                StartRoaming();
            }
            return 0;
        }

        public void Destroy()
        {
            if(Sprite != null)
                SpriteManager.RemoveSprite(Sprite);
            if(Label != null)
                TextManager.RemoveText(Label);
            ShapeManager.Remove(this);
        }
    }
}

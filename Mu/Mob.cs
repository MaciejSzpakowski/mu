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
        private float MovingSpeed;
        public MobClass Class;
        public bool Removed;
        private Vector3 zLastTarget;
        public MobMap Map;
        public MapRoom Room;
        private float SenseDistance;
        private float AttackDistance;
        private TimeSpan AttackSpeed;
        public ServerClient HeroTarget;

        private Mob()
        {
            MovingSpeed = 6;
            Removed = false;
            Netid = 0;
            State = MobState.Roaming;
            SenseDistance = 8;
            AttackDistance = 2;
            AttackSpeed = TimeSpan.FromSeconds(2);
            HeroTarget = null;
            ShapeManager.AddCircle(this);
        }

        public static Mob MobServer(MobClass mobclass, MobMap map, Vector2 pos)
        {
            Mob m = new Mob();
            m.Target = m.Position = new Vector3(pos, ZLayer.Npc);
            m.Map = map;
            m.Netid = NextNetid++;
            m.Class = mobclass;
            m.StartRoaming();
            m.StartUpdatingPos();
            return m;
        }

        public static Mob MobClient(MobClass mobclass, Vector2 pos)
        {
            Mob m = new Mob();
            m.Class = mobclass;
            m.Target = m.Position = new Vector3(pos, ZLayer.Npc);
            if(!Debug.DebugMode)
                m.Visible = false;
            m.Color = Color.Red;
            m.InitClass();
            m.InitSprite(m.File);
            m.InitLabel();
            return m;
        }

        public void ServerActivity()
        {
            GotoTarget();
            //restore removed mobs
            if (Removed)
            {
                ShapeManager.AddCircle(this);
                Removed = false;
            }
            switch (State)
            {
                case MobState.Roaming:
                    ServerRoaming();
                    break;
                case MobState.Chasing:
                    ServerChasing();
                    break;
                case MobState.Atacking:
                    ServerAttacking();
                    break;
                default:
                    break;
            }
        }

        private void ServerRoaming()
        {
            //find target thats close enough            
            ServerClient target = Room.Clients.FirstOrDefault(c =>
            (Position - c.Hero.Position).LengthSquared() < SenseDistance * SenseDistance);
            if (target != null)
            {
                State = MobState.Chasing;
                HeroTarget = target;
            }
        }

        private void ServerChasing()
        {
            Target = HeroTarget.Hero.Position;

            //stop chasing if it's too far
            if (HeroTarget == null || 
                (Position - HeroTarget.Hero.Position).LengthSquared() > SenseDistance * SenseDistance)
            {
                State = MobState.Roaming;
                HeroTarget = null;
                return;
            }

            //start attacking if it's close enough
            else if ((Position - HeroTarget.Hero.Position).LengthSquared() < AttackDistance * AttackDistance)
            {
                State = MobState.Atacking;
                Target = Position;
                Globals.EventManager.AddEvent(AttackHero, $"attacking{Netid}", 
                    false, TimeSpan.Zero, TimeSpan.Zero, AttackSpeed);
            }            
        }

        private void ServerAttacking()
        {
            if (HeroTarget == null)
            {
                State = MobState.Roaming;
                Globals.EventManager.RemoveEvent($"attacking{Netid}");
                return;
            }

            //stop attacking and start chasing if it's too far
            //*1.5f is there so mob doesnt attack from the border of its range but rather from inside
            //so player cant make a small step to force it to chase again
            else if ((Position - HeroTarget.Hero.Position).LengthSquared() > 
                AttackDistance * AttackDistance * 1.5f)
            {
                Globals.EventManager.RemoveEvent($"attacking{Netid}");
                State = MobState.Chasing;
                return;
            }
        }

        private int AttackHero()
        {
            return 1;
        }

        public void ClientActivity()
        {
            GotoTarget();
            AnimationControl();
        }

        private bool FlipHorizontal = false;
        public void AnimationControl()
        {
            //set to walk if non-zero V
            if ((Velocity != Vector3.Zero) && Sprite.CurrentChainName != "Walk")
            {
                float walkingAnimationSpeed = 0.33f;
                Sprite.AnimationSpeed = walkingAnimationSpeed;
                Sprite.CurrentChainName = "Walk";
            }
            //attack
            else if (Sprite.CurrentChainName == "Attack")
            {
                if (Sprite.JustCycled)
                {
                    Sprite.CurrentChainName = "Idle";
                }
            }
            //set to idle if zero V
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

        public void SetName(string name, Color color)
        {
            Name = name;
            Label.DisplayText = name;
            Label.SetColor(color);
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
            //collect if its killed
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
            if (State == MobState.Killed)
                return 0;
            //set roam target only if its state is roaming
            if (State == MobState.Roaming)
                Target = Position + new Vector3(Rng.NextFloat(-4, 4), Rng.NextFloat(-4, 4), 0); 
            StartRoaming();
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

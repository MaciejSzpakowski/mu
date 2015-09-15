using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Screens;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mu
{
    public class DamageCircle : Circle
    {
        int Id;
        public Hero HeroOwner;
        public Mob MobOwner;
        public bool DestroyOnHit;
        public MapRoom Room;

        public DamageCircle(Hero heroOwner, Mob mobOwner, bool destroyOnHit, int id, MapRoom room)
        {
            Id = id;
            HeroOwner = heroOwner;
            MobOwner = mobOwner;
            DestroyOnHit = destroyOnHit;
            Room = room;
            ShapeManager.AddCircle(this);
        }

        public void Destroy()
        {
            ShapeManager.Remove(this);
        }
    }

    public class DamageCircleManager
    {
        private PositionedObjectList<DamageCircle> Circles;
        private int NextId;
        public static float HeroRadius = 1;

        public DamageCircleManager()
        {
            NextId = 0;
            Circles = new PositionedObjectList<DamageCircle>();
        }

        public DamageCircle AddCircle(Hero heroOwner, Mob mobOwner, TimeSpan lifeSpan, 
            bool destroyOnHit, MapRoom room)
        {
            DamageCircle c = new DamageCircle(heroOwner, mobOwner, destroyOnHit, NextId++, room);
            Circles.Add(c);
            //remove event
            Globals.EventManager.AddEvent(delegate ()
            {
                c.Destroy();
                return 0;
            }, $"removecircle{NextId - 1}", false, lifeSpan, TimeSpan.Zero, TimeSpan.Zero);
            return c;
        }

        public void ActivityServer()
        {
            foreach (DamageCircle c in Circles)
                foreach (ServerClient h in c.Room.Clients)
                    if (Collide(c, h.Hero))
                        h.SendHitPlayer(c.MobOwner);
        }

        /// <summary>
        /// collision simulation because clienthero is not a shape to collide against
        /// </summary>
        /// <param name="c"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public bool Collide(DamageCircle c, ClientHero h)
        {
            return Vector3.DistanceSquared(c.Position, h.Position) <
                (c.Radius + HeroRadius) * (c.Radius + HeroRadius);
        }
    }
}

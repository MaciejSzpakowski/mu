using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Input;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Screens;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mu
{
    public enum HeroClass { Elf, Knight, Wizard, Invalid };

    public struct HeroStats
    {

    }

    public class Hero : PositionedObject
    {
        private Circle zCollider;
        private Sprite zSprite;        
        private Text zLabel;
        private Sprite zHealthBar;        
        private HeroStats zStats;

        //serializable
        public HeroClass Class;
        public float ExperienceBoost;
        public TimeSpan Online; //how much time hero has been played
        public string Map; //what map is it on
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
            Name = name;
            Class = heroClass;
            zCollider = ShapeManager.AddCircle();
            zCollider.AttachTo(this, false);
            zSprite = LoadSprite();
            zSprite.AttachTo(this, false);
            zLabel = TextManager.AddText(name, Globals.Font);
            zLabel.AttachTo(this, false);

            InitStats();
        }

        private Sprite LoadSprite()
        {
            if (Class == HeroClass.Elf)
                return Functions.AddSpriteFromAchx(Path.MakePath(Path.Texture, "elf.achx"));
            else if (Class == HeroClass.Knight)
                return Functions.AddSpriteFromAchx(Path.MakePath(Path.Texture, "knight.achx"));
            else if (Class == HeroClass.Wizard)
                return Functions.AddSpriteFromAchx(Path.MakePath(Path.Texture, "wizard.achx"));
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
            if (Class == HeroClass.Elf)
            {
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
            ShapeManager.Remove(zCollider);
            SpriteManager.RemoveSprite(zSprite);
            SpriteManager.RemoveSprite(zHealthBar);
            TextManager.RemoveText(zLabel);
        }
    }

    [Serializable]
    public class SaveHero
    {
        public string Map;
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

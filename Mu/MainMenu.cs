using FlatRedBall;
using FlatRedBall.Graphics;
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
    public class MainMenu : Screen
    {
        public MainMenu()
            : base("MainMenu")
        {
        }

        private List<MenuCharacter> zChars;
        private MenuCharacter zSelectedChar;

        public class MenuCharacter : Window
        {
            public string zFileName;
            public string zHeroName;

            public MenuCharacter(string fileName)
            {
                Immovable = true;
                zFileName = fileName;
                SaveHero shero = (SaveHero)Functions.Deserialize(fileName);
                zHeroName = shero.Name;
                Text = shero.Name + " "+ shero.Level.ToString();
                zText.RelativePosition.Y += 3;
                zText.HorizontalAlignment = HorizontalAlignment.Center;
                var s1 = SetSprite(shero);
                SpriteManager.RemoveSprite(s1);
                zSprite.Texture = s1.Texture;
                zSprite.LeftTextureCoordinate = 0.333f;
                zSprite.RightTextureCoordinate = 0.667f;
                zSprite.ColorOperation = ColorOperation.Texture;
                Size = new Vector2(8, 8);
                OnClick = delegate ()
                {
                    MainMenu m = (MainMenu)ScreenManager.CurrentScreen;
                    m.SelectCharacter(this);
                };
            }

            private Sprite SetSprite(SaveHero shero)
            {
                switch (shero.Class)
                {
                    case HeroClass.Elf:
                        return Functions.AddSpriteFromAchx(Path.MakePath(Path.Texture, "elf.achx"));
                    case HeroClass.Knight:
                        return Functions.AddSpriteFromAchx(Path.MakePath(Path.Texture, "knight.achx"));
                    case HeroClass.Wizard:
                        return Functions.AddSpriteFromAchx(Path.MakePath(Path.Texture, "wizard.achx"));
                    default:
                        return null;
                }
            }

            private void SelectCharacter()
            {

            }
        }

        public class NewCharacterWindow : Window
        {
            private HeroClass zHeroClass;
            private TextBox zNametextbox;

            public NewCharacterWindow():base(null,true)
            {
                zHeroClass = HeroClass.Invalid;
                InitProps(new Vector2(-11, 5), new Vector2(22, 10), new Color(0.3f, 0.3f, 0.3f, 0.75f), "", Color.White);
                CloseWithEscape = true;

                zNametextbox = new TextBox(this);
                zNametextbox.InitProps(Position + new Vector2(1, -7), new Vector2(14, 1.5f), new Color(0.1f, 0.1f, 0.1f, 1), "", Color.White);
                zNametextbox.MaxLength = 18;

                Window elfbutton = new Button(this);
                elfbutton.InitProps(Position + new Vector2(1, -1), new Vector2(4, 2), new Color(0.1f, 0.1f, 0.1f, 1), "Elf", Color.White);
                elfbutton.OnClick = delegate ()
                {
                    zHeroClass = HeroClass.Elf;
                };

                Window knightbutton = new Button(this);
                knightbutton.InitProps(elfbutton.Position + new Vector2(5, 0), new Vector2(4, 2), new Color(0.1f, 0.1f, 0.1f, 1), "Knight", Color.White);
                knightbutton.OnClick = delegate ()
                {
                    zHeroClass = HeroClass.Knight;
                };

                Window wizardbutton = new Button(this);
                wizardbutton.InitProps(knightbutton.Position + new Vector2(5, -0), new Vector2(4, 2), new Color(0.1f, 0.1f, 0.1f, 1), "Wizard", Color.White);
                wizardbutton.OnClick = delegate ()
                {
                    zHeroClass = HeroClass.Wizard;
                };

                Window createbutton = new Button(this);
                createbutton.InitProps(Position + new Vector2(16.5f, -7.5f), new Vector2(4, 2), new Color(0.1f, 0.1f, 0.1f, 1), "Create", Color.White);
                createbutton.OnClick = CreateNewCharacter;

                Window charSprite = new Window(this);
                charSprite.InitProps(new Vector2(17, 4), new Vector2(4, 4), Color.White, "", Color.White);
                charSprite.Visible = false;
            }

            private void CreateNewCharacter()
            {
                if (!IsNameValid())
                {
                    string msg = "Invalid name\n*letter, number and spaces only\n*cannot begin or end with space\n*no 2 spaces in a row\n*min 3 characters";
                    new MessageBox(msg);
                    return;
                }
                if (zHeroClass == HeroClass.Invalid)
                {
                    new MessageBox("Select class");
                    return;
                }

                string fileName = Path.MakePath(Path.Save, zNametextbox.Text);
                fileName = fileName.Replace(" ", "_");
                fileName += ".hero";

                if (System.IO.File.Exists(Path.MakePath(Path.Save, fileName)))
                {
                    new MessageBox("This name is taken");
                    return;
                }

                Hero hero = new Hero(zNametextbox.Text, zHeroClass);
                SaveHero shero = hero.ToSavehero();
                Functions.Serialize(fileName, shero);
                hero.Destroy();
                MainMenu m = (MainMenu)ScreenManager.CurrentScreen;
                m.ReloadCharacterList();
            }

            private bool IsNameValid()
            {
                string name = zNametextbox.Text;
                if (!Regex.IsMatch(name, "^[A-Za-z0-9][A-Za-z0-9 ]+[A-Za-z0-9]$"))
                    return false;
                if (Regex.IsMatch(name, "[ ][ ]"))
                    return false;
                return true;
            }            
        }

        public class PlayWindow : Window
        {
            private TextBox zServer;
            private TextBox zPort;

            public PlayWindow() : base(null, true)
            {
                InitProps(new Vector2(-11, 5), new Vector2(22, 10), new Color(0.3f, 0.3f, 0.3f, 0.75f), "", Color.White);
                CloseWithEscape = true;

                zServer = new TextBox(this);
                zServer.InitProps(Position + new Vector2(1, -1), new Vector2(10, 2), new Color(0.1f, 0.1f, 0.1f, 1), "", Color.White);

                zPort = new TextBox(this);
                zPort.InitProps(Position + new Vector2(1, -4), new Vector2(10, 2), new Color(0.1f, 0.1f, 0.1f, 1), "", Color.White);

                Window playbutton = new Button(this);
                playbutton.InitProps(Position + new Vector2(1, -7), new Vector2(4, 2), new Color(0.1f, 0.1f, 0.1f, 1), "Play", Color.White);

                Window closebutton = new Button(this);
                closebutton.InitProps(Position + new Vector2(6, -7), new Vector2(4, 2), new Color(0.1f, 0.1f, 0.1f, 1), "Back", Color.White);
                closebutton.OnClick = delegate ()
                {
                    Destroy();
                };
            }
        }

        public class MainWindow : Window
        {
            public MainWindow() : base()
            {
                InitProps(new Vector2(-21, 16), new Vector2(10, 16), new Color(0.3f, 0.3f, 0.3f, 0.75f), "", Color.White);

                Window newCharButton = new Button(this);
                newCharButton.InitProps(Position + new Vector2(1, -1), new Vector2(8, 2), new Color(0.1f, 0.1f, 0.1f, 1), "New Character", Color.White);
                newCharButton.OnClick = delegate ()
                {
                    new NewCharacterWindow();
                };

                Window playbutton = new Button(this);
                playbutton.InitProps(Position + new Vector2(1, -4), new Vector2(8, 2), new Color(0.1f, 0.1f, 0.1f, 1), "Play", Color.White);                
                playbutton.Visible = false;
                playbutton.OnClick = delegate ()
                {
                    new PlayWindow();
                };

                Window delcharbutton = new Button(this);
                delcharbutton.InitProps(Position + new Vector2(1, -7), new Vector2(8, 2), new Color(0.4f, 0.1f, 0.1f, 1), "Delete Character", Color.White);
                delcharbutton.Visible = false;
                delcharbutton.OnClick = delegate ()
                {
                    MainMenu m = (MainMenu)ScreenManager.CurrentScreen;
                    m.DeleteCharacterRoutine();
                };
                //event set visible if sometihng is selected
                Globals.EventManager.AddEvent(delegate ()
                {
                    MainMenu m = (MainMenu)ScreenManager.CurrentScreen;
                    delcharbutton.Visible = m.zSelectedChar != null;
                    playbutton.Visible = m.zSelectedChar != null;
                    return 1;
                }, "delcharbuttonvisibility");

                Window optionsbutton = new Button(this);
                optionsbutton.InitProps(Position + new Vector2(1, -10), new Vector2(8, 2), new Color(0.1f, 0.1f, 0.1f, 1), "Options", Color.White);
                optionsbutton.OnClick = delegate ()
                {
                    new OptionsMenu();
                };

                Window exitbutton = new Button(this);
                exitbutton.InitProps(Position + new Vector2(1, -13), new Vector2(8, 2), new Color(0.1f, 0.1f, 0.1f, 1), "Exit", Color.White);
                exitbutton.OnClick = delegate ()
                {
                    Globals.Game.Exit();
                };
            }

            public override void Destroy()
            {
                Globals.EventManager.RemoveEvent("delcharbuttonvisibility");
                base.Destroy();
            }
        }

        public override void Initialize(bool addToManagers)
        {
            //i think this should be first
            base.Initialize(addToManagers);
            TestInit();
            zChars = new List<MenuCharacter>();
            zSelectedChar = null;
            LoadChars();
            //main window
            new MainWindow();            
        }

        private void LoadChars()
        {
            int maxCharacters = 16;            
            float initX = -9;
            float initY = 15;
            float deltaX = 7;
            float deltaY = -7;
            Vector2 pos = new Vector2(initX, initY);
            var sortedFileInfos = new System.IO.DirectoryInfo(Path.Save).GetFiles().OrderBy(file => file.CreationTime).ToArray();
            string[] sortedFiles = sortedFileInfos.Select(file => file.FullName).ToArray();
            
            for (int i = 0; i < sortedFiles.Length && i < maxCharacters; i++)
            {
                zChars.Add(new MenuCharacter(sortedFiles[i]));
                zChars.Last().Position = pos;
                //get next pos
                pos.X += deltaX;
                if ((i+1) % 4 == 0)
                {
                    pos.X = initX;
                    pos.Y += deltaY;
                }
            }
        }

        private void ReloadCharacterList()
        {
            foreach (MenuCharacter m in zChars)
                m.Destroy();
            zChars.Clear();
            LoadChars();
        }

        private void SelectCharacter(MenuCharacter m)
        {
            if (zSelectedChar != null)
                zSelectedChar.TextColor = Color.White;
            zSelectedChar = m;
            m.TextColor = Color.Red;
        }

        private void DeleteCharacterRoutine()
        {
            new MessageBox("Delete this char ?\n" + zSelectedChar.zHeroName, MessageBoxType.YesNo);
            Globals.EventManager.AddEvent(delegate ()
            {
                if (Globals.GuiManager.LastMessageBoxReturn == MessageBoxReturn.YES)
                {
                    DeleteCharacter();
                    return 0;
                }
                else if (Globals.GuiManager.LastMessageBoxReturn == MessageBoxReturn.NO)
                    return 0;
                return 1;
            }, "delcharmb");
        }

        private void DeleteCharacter()
        {
            System.IO.File.Delete(zSelectedChar.zFileName);
            ReloadCharacterList();
            zSelectedChar = null;
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
            foreach (MenuCharacter m in zChars)
                m.Destroy();
            base.Destroy();
        }

        public void TestInit()
        {
        }

        public void TestActivity()
        {
            var cur = new Vector2(InputManager.Mouse.WorldXAt(0), InputManager.Mouse.WorldYAt(0));
            Globals.Debug(cur.X, "x");
            Globals.Debug(cur.Y, "y");
        }
    }
}

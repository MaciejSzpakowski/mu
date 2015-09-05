using FlatRedBall;
using FlatRedBall.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using static FlatRedBall.Input.InputManager;
using static FlatRedBall.Input.Mouse;
using FlatRedBall.Gui;

namespace Mu
{
    public delegate void VoidFunction();

    public class Window
    {
        public string Name; //to find the window easly
        public Text zText;
        public Sprite zSprite;
        public bool zVisible;
        public VoidFunction OnClick;
        public VoidFunction Hover;
        public VoidFunction HoverMousedown;
        public VoidFunction MouseLeave;
        public VoidFunction MouseEnter;
        public Window zParent;
        public Layer zLayer;
        public List<Window> zChildren;
        protected Vector2 zOrigin; //i need this crap to maintain correct offset from origin
        public Sprite Sprite { get { return zSprite; } }
        public bool zModal;
        public bool Immovable;
        private Event zEventEscape;
        protected bool Collect; //used to signal running events that this window has been destroyed

        public Window(Window owner = null, bool modal = false, string sprite = "")
        {            
            if (owner != null && modal)
                throw new ArgumentException("Child cannot be modal");
            Collect = false;
            Immovable = false;
            zEventEscape = null;
            zModal = modal;
            Name = string.Empty;
            zOrigin = new Vector2(0, 0);
            if (sprite == "")
                zSprite = SpriteManager.AddSprite(Path.Make(Path.Misc, "pixel.bmp"));
            else
                zSprite = SpriteManager.AddSprite(sprite);
            zSprite.ColorOperation = ColorOperation.ColorTextureAlpha;
            Color = Color.White;
            zVisible = true;
            OnClick = delegate () { };
            Hover = delegate () { };
            HoverMousedown = delegate () { };
            MouseLeave = delegate () { };
            MouseEnter = delegate () { };
            zParent = owner;
            zChildren = new List<Window>();
            zText = TextManager.AddText(string.Empty, Globals.Font);
            zText.ColorOperation = ColorOperation.ColorTextureAlpha;
            zText.AttachTo(zSprite, false);
            Size = new Vector2(1, 1);
            if (owner == null)
                InitializeAsTopLevel();
            else
                InitializeAsChild();
            SpriteManager.AddToLayer(zSprite, zLayer);
            TextManager.AddToLayer(zText, zLayer);
            zSprite.AttachTo(Camera.Main, true);
        }

        protected void InitializeAsChild()
        {
            zLayer = zParent.zLayer;
            zParent.zChildren.Add(this);
            Position = zParent.Position;
        }

        protected void InitializeAsTopLevel()
        {            
            zLayer = SpriteManager.AddLayer();
            if (zModal)
            {
                BringToFront();
                Globals.GuiManager.AddModalWindow(this);
            }
            else
                Globals.GuiManager.AddWindow(this);
        }

        public virtual void Activity()
        {

        }

        /// <summary>
        /// Returns true if this.Layer is higer than w.Layer
        /// </summary>
        /// <param name="w">window to compare to</param>
        /// <returns></returns>
        public bool IsHigherThan(Window w)
        {
            return SpriteManager.Layers.IndexOf(zLayer) > SpriteManager.Layers.IndexOf(w.zLayer);
        }

        public void BringToFront()
        {
            SpriteManager.MoveToFront(zLayer);
        }        

        /// <summary>
        /// Recursive show
        /// </summary>
        public void Show()
        {
            zVisible = true;
            zSprite.Visible = true;
            foreach (Window w in zChildren)
                w.Show();
        }

        /// <summary>
        /// Recursive hide
        /// </summary>
        public void Hide()
        {
            zVisible = false;
            zSprite.Visible = false;
            foreach (Window w in zChildren)
                w.Hide();
        }        

        /// <summary>
        /// Do stuff like:
        /// call OnClick if window cliked, make it focued, drag etc.
        /// used only by GuiManager
        /// m in front of he name added to obscure
        /// returns wheter window is being dragged or not
        /// </summary>
        public void ExecuteMouseRoutine()
        {
            Hover();
            if (Mouse.ButtonPushed(MouseButtons.LeftButton))
            {
                if (zParent == null)
                    Functions.ClipCursorInGameWindow();
                Focus();
                Globals.GuiManager.zDoGetWindows = false;
                Globals.EventManager.AddEvent(ClickRoutine, "WindowMouseRoutine");

            }
        }

        protected int ClickRoutine()
        {
            if (Collect)
                return 0;
            HoverMousedown();
            if (zParent != null && !IsUnderCursor())
            {
                Globals.GuiManager.zDoGetWindows = true;
                return 0;
            }
            if (Mouse.ButtonReleased(MouseButtons.LeftButton))
            {
                Functions.UnclipCursor();
                OnClick();
                Globals.GuiManager.zDoGetWindows = true;
                return 0;
            }
            if (zParent == null && !Immovable)
                Drag();
            return 1;
        }

        /// <summary>
        /// Checks wheter this windows is suitable for dragging and drags if it is
        /// suitable window: top level only, it recevied click and lmb is currently down
        /// </summary>
        protected void Drag()
        {
            Position += new Vector2(Mouse.WorldXChangeAt(0), -Mouse.WorldYChangeAt(0));
        }

        /// <summary>
        /// //mark for drag, click and bring window to front
        /// </summary>
        public virtual void Focus()
        {
            BringToFront();
        }

        public bool IsUnderCursor() => Visible && GuiManager.Cursor.IsOn(zSprite);

        public void InitProps(Vector2 pos, Vector2 size, Color color, string text, Color textColor)
        {
            Position = pos;
            Size = size;
            Color = color;
            Text = text;
            TextColor = textColor;
        }

        /// <summary>
        /// Centers text, buttons need that
        /// </summary>
        public void CenterText()
        {
            zText.HorizontalAlignment = HorizontalAlignment.Center;
            zText.VerticalAlignment = VerticalAlignment.Center;
            zText.RelativePosition = new Vector3(0, -zText.Height/3f, 0.001f);
        }

        /// <summary>
        /// This should be called on top level window, it will clean all children
        /// </summary>
        public virtual void Destroy()
        {
            Collect = true;
            if (zParent != null)
                throw new AccessViolationException("Window.Destroy() can be called on top level window only");
            SpriteManager.RemoveSprite(zSprite);
            TextManager.RemoveText(zText);
            foreach (Window w in zChildren)
                w.DestroyChild();
            SpriteManager.RemoveLayer(zLayer);
            zChildren.Clear();
            if (zModal)
                Globals.GuiManager.RemoveModalWindow(this);
            else
                Globals.GuiManager.RemoveWindow(this);

        }

        /// <summary>
        /// This should be called only when editing content of a window
        /// </summary>
        public void DestroyChild()
        {
            Collect = true;
            if (zParent == null)
                throw new AccessViolationException("Window.DestroyChild() can be called on child window only");

            SpriteManager.RemoveSprite(zSprite);
            TextManager.RemoveText(zText);
            foreach (Window w in zChildren)
                w.DestroyChild();
            zChildren.Clear();
        }

        public Vector2 Position
        {
            get { return zOrigin; }
            set
            {
                Vector2 diff = value - zOrigin;
                zOrigin = value;
                Sprite.RelativePosition = new Vector3(zOrigin.X + zSprite.ScaleX,
                    zOrigin.Y - zSprite.ScaleY, zSprite.Position.Z - 40);
                foreach (Window w in zChildren)
                    w.Position += diff;
            }
        }

        public Color Color
        {
            get { return new Color(zSprite.Red, zSprite.Green, zSprite.Blue, zSprite.Alpha); }
            set
            {
                zSprite.Red = value.R/255f;
                zSprite.Green = value.G/255f;
                zSprite.Blue = value.B/255f;
                zSprite.Alpha = value.A/255f;
            }
        }

        public Vector2 Size
        {
            get { return new Vector2(zSprite.ScaleX * 2, zSprite.ScaleY * 2); }
            set
            {
                value.X /= 2;
                value.Y /= 2;
                zSprite.ScaleX = value.X;
                zSprite.ScaleY = value.Y;
                //realign, position changes which is important to display window correctly
                Position = Position;
                //realign text since now windows looks different
                if (zText.HorizontalAlignment == HorizontalAlignment.Left)
                    zText.RelativePosition = new Vector3(-zSprite.ScaleX + 0.2f, zSprite.ScaleY - 0.2f - zText.Height / 2, 0.001f);
            }
        }

        public bool Visible
        {
            get { return zVisible; }
            set
            {
                zVisible = value;
                if (value == false)
                    Hide();
                else
                    Show();
            }
        }

        public float Transparency
        {
            get { return zSprite.Alpha; }
            set { zSprite.Alpha = value; }
        }

        public virtual string Text
        {
            get { return zText.DisplayText; }
            set { zText.DisplayText = value; }
        }

        public Color TextColor
        {
            get { return zText.GetColor(); }
            set { zText.SetColor(value.R/255f, value.G/255f, value.B/255f); zText.Alpha = value.A/255f; }
        }

        public bool CloseWithEscape
        {
            get { return zEventEscape != null; }
            set
            {
                if (value && zEventEscape == null)
                {
                    zEventEscape = Globals.EventManager.AddEvent(zCloseWithEscape, "zclose" + GetHashCode().ToString(),true);
                }
                else if (!value && zEventEscape != null)
                {
                    Globals.EventManager.RemoveEvent(zEventEscape);
                    zEventEscape = null;
                }
            }
        }

        private int zCloseWithEscape()
        {
            if (Collect)
                return 0;
            if (Keyboard.KeyPushed(Keys.Escape))
            {
                Keyboard.IgnoreKeyForOneFrame(Keys.Escape);
                Destroy();                
                return 0;
            }
            return 1;
        }
    }

    public class TextBox : Window
    {
        public VoidFunction OnEnter;
        public uint MaxLength;
        private char carret;
        private string input;
        private bool zAutoScale;
        private float zOriginalScalex;
        private bool zTypinRoutineRunning;

        public TextBox(Window owner) : base(owner)
        {
            zTypinRoutineRunning = false;
            zAutoScale = false;
            input = string.Empty;
            carret = (char)300;
            MaxLength = uint.MaxValue;
            OnEnter = delegate () { };
            OnClick = StartTyping;
            zText.RelativePosition.X = 0.5f;
            zText.RelativePosition.Y = -zSprite.ScaleY;
            Globals.EventManager.AddEvent(AutoScalling, "autoscalling");
            zOriginalScalex = Size.X;
        }

        public void StartTyping()
        {
            if (!Globals.EventManager.HasEvent("typingroutine"))
            {
                zTypinRoutineRunning = true;
                Globals.EventManager.AddEvent(TypingRoutine, "typingroutine");
            }
        }

        public bool IsTyping() => zTypinRoutineRunning;

        private int TypingRoutine()
        {
            if (Collect)
            {
                zTypinRoutineRunning = false;
                return 0;
            }
            input += Keyboard.GetStringTyped();
            //limit
            if (input.Length > MaxLength)
                input = input.Remove((int)MaxLength);
            //backspace if length > 0
            if (input.Length > 0 && 
                Keyboard.KeyTyped(Keys.Back))
                input = input.Remove(input.Length - 1);
            zText.DisplayText = input;
            CarretActivity();
            //end routine if clicked
            if (Mouse.ButtonPushed(MouseButtons.LeftButton))
            {
                RemoveCarret();
                zTypinRoutineRunning = false;
                return 0;
            }
            //escape cancels typing
            if (Keyboard.KeyPushed(Keys.Escape))
            {
                Keyboard.IgnoreKeyForOneFrame(Keys.Escape);
                Text = string.Empty;
                zTypinRoutineRunning = false;
                return 0;
            }
            //onenter
            if (Keyboard.KeyPushed(Keys.Enter))
            {
                RemoveCarret();
                OnEnter();
                zTypinRoutineRunning = false;
                return 0;
            }
            return 1;
        }

        private int AutoScalling()
        {
            if (Collect)
                return 0;
            if (!zAutoScale)
                return 1;
            if(Size.X > zOriginalScalex)
                Size = new Vector2(zOriginalScalex, Size.Y);
            if (zText.Width + 1 > Size.X)
                Size = new Vector2(zText.Width + 1, Size.Y);
            return 1;
        }

        private void RemoveCarret()
        {
            if (zText.DisplayText.Length != 0 && zText.DisplayText.Last() == carret)
                zText.DisplayText = zText.DisplayText.Remove(zText.DisplayText.Length - 1);
        }

        private void CarretActivity()
        {
            if (Globals.GameTime.TotalGameTime.Milliseconds > 500)
                AddCarret();
            else
                RemoveCarret();
        }

        private void AddCarret()
        {
            if (zText.DisplayText.Length == 0 || zText.DisplayText.Last() != carret)
                zText.DisplayText += carret.ToString();
        }

        public override void Focus()
        {
            StartTyping();
            base.Focus();
        }

        public bool AutoScale
        {
            get
            {
                return zAutoScale;
            }
            set
            {
                zOriginalScalex = Size.X;
                zAutoScale = value;
            }
        }

        public override string Text
        {
            get
            {
                return input;
            }

            set
            {
                input = value;
                zText.DisplayText = input;
            }
        }
    }

    public class Button : Window
    {
        public Button(Window owner, string sprite=""):base(owner,false, sprite)
        {
            CenterText();
            MouseEnter = delegate ()
            {
                TextColor = Color.Red;
            };
            MouseLeave = delegate ()
            {
                TextColor = Color.White;
            };
        }
    }

    public enum MessageBoxType { OK, YesNo };
    public enum MessageBoxReturn { OK, YES, NO, Nothing };

    public class MessageBox : Window
    {
        public MessageBox(string text, MessageBoxType type = MessageBoxType.OK, string msg = "") : base(null,true)
        {
            Position = new Vector2(-7, 5);
            Size = new Vector2(14, 10);
            Color = new Color(1, 0, 0, 0.85f);
            Text = text;
            zText.VerticalAlignment = VerticalAlignment.Top;
            TextColor = Color.White;
            Globals.GuiManager.LastMessageBoxReturn = MessageBoxReturn.Nothing;
            Globals.GuiManager.LastMbMessage = msg;

            if (type == MessageBoxType.OK)
                InitOk();
            else if (type == MessageBoxType.YesNo)
                InitYesno();
        }

        private void InitOk()
        {
            Window ok = new Button(this);
            ok.Size = new Vector2(4, 2);
            ok.Position = Position + new Vector2(5, -7);
            ok.Text = "OK";
            ok.Color = new Color(0.1f, 0.1f, 0.1f, 1);
            ok.OnClick = OkClick;
            ok.TextColor = Color.White;
            Globals.EventManager.AddEvent(MbEscape, "mbescape", true);
        }

        private void InitYesno()
        {
            Window yes = new Button(this);
            yes.Size = new Vector2(4, 2);
            yes.Position = Position + new Vector2(2, -7);
            yes.Text = "YES";
            yes.Color = new Color(0.1f, 0.1f, 0.1f, 1);
            yes.OnClick = YesClick;
            yes.TextColor = Color.White;

            Window no = new Button(this);
            no.Size = new Vector2(4, 2);
            no.Position = Position + new Vector2(8, -7);
            no.Text = "NO";
            no.Color = new Color(0.1f, 0.1f, 0.1f, 1);
            no.OnClick = NoClick;
            no.TextColor = Color.White;
            Globals.EventManager.AddEvent(MbEscape, "mbescape", true);
        }

        private int MbEscape()
        {
            if (Collect)
                return 0;
            if (Keyboard.KeyPushed(Keys.Escape))
            {
                Keyboard.IgnoreKeyForOneFrame(Keys.Escape);
                Globals.GuiManager.LastMessageBoxReturn = MessageBoxReturn.NO;
                Destroy();
                return 0;
            }
            return 1;
        }

        private void OkClick()
        {
            Globals.GuiManager.LastMessageBoxReturn = MessageBoxReturn.OK;
            Destroy();
        }

        private void YesClick()
        {
            Globals.GuiManager.LastMessageBoxReturn = MessageBoxReturn.YES;
            Destroy();
        }

        private void NoClick()
        {
            Globals.GuiManager.LastMessageBoxReturn = MessageBoxReturn.NO;
            Destroy();
        }

        public override void Destroy()
        {
            base.Destroy();
        }
    }
}

using FlatRedBall;
using FlatRedBall.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOPath = System.IO.Path;
using FlatRedBall.Input;
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
        public VoidFunction MouseEnter;
        public VoidFunction MouseLeave;
        public Window zParent;
        public Layer zLayer;
        public List<Window> zChildren;
        public Vector2 zOrigin; //i need this crap to maintain correct offset from origin
        public Sprite Sprite { get { return zSprite; } }
        public bool zModal;
        public bool Immovable;

        public Window(Window owner = null, bool modal = false, string sprite = "")
        {            
            if (owner != null && modal)
                throw new ArgumentException("Child cannot be modal");
            Immovable = false;
            zModal = modal;
            Name = string.Empty;
            zOrigin = new Vector2(0, 0);
            if (sprite == "")
                zSprite = SpriteManager.AddSprite(IOPath.Combine(Path.Texture, "pixel.bmp"));
            else
                zSprite = SpriteManager.AddSprite(sprite);
            zSprite.ColorOperation = ColorOperation.ColorTextureAlpha;
            Color = Color.White;
            zVisible = true;
            OnClick = delegate () { };
            Hover = delegate () { };
            HoverMousedown = delegate () { };
            MouseEnter = delegate () { };
            MouseLeave = delegate () { };
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
                MoveToFront();
                Globals.GuiManager.AddModalWindow(this);
            }
            else
                Globals.GuiManager.AddWindow(this);
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

        public void MoveToFront()
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
            if (InputManager.Mouse.ButtonPushed(Mouse.MouseButtons.LeftButton))
            {
                if(zParent == null)
                    Functions.ClipCursorInGameWindow();
                Focus();
                Globals.GuiManager.zDoGetWindows = false;
                Globals.EventManager.AddEvent(ClickRoutine, "WindowMouseRoutine", 0, 0, 0);

            }
        }

        protected int ClickRoutine()
        {
            HoverMousedown();
            if (zParent != null && !IsUnderCursor())
            {
                Globals.GuiManager.zDoGetWindows = true;
                return 0;
            }
            if (!InputManager.Mouse.ButtonDown(Mouse.MouseButtons.LeftButton))
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
            Position += new Vector2(InputManager.Mouse.WorldXChangeAt(0), -InputManager.Mouse.WorldYChangeAt(0));
        }

        /// <summary>
        /// //mark for drag, click and bring window to front
        /// </summary>
        protected void Focus()
        {
            MoveToFront();
        }

        public bool IsUnderCursor()
        {
            return Visible && GuiManager.Cursor.IsOn(zSprite);
        }

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
            zText.RelativePosition = new Vector3(0, 0, 0.001f);
        }

        /// <summary>
        /// This should be called on top level window, it will clean all children
        /// </summary>
        public virtual void Destroy()
        {
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
                Sprite.Position = new Vector3(zOrigin.X + zSprite.ScaleX,
                    zOrigin.Y - zSprite.ScaleY, zSprite.Position.Z);
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
                if(zText.HorizontalAlignment == HorizontalAlignment.Left)
                    zText.RelativePosition = new Vector3(-zSprite.ScaleX, zSprite.ScaleY - zText.Height / 2, 0.001f);
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

        public string Text
        {
            get { return zText.DisplayText; }
            set { zText.DisplayText = value; }
        }

        public Color TextColor
        {
            get { return zText.GetColor(); }
            set { zText.SetColor(value.R/255f, value.G/255f, value.B/255f); zText.Alpha = value.A/255f; }
        }
    }

    public class TextBox : Window
    {
        public VoidFunction OnEnter;
        public uint MaxLength;

        public TextBox(Window owner) : base(owner)
        {
            MaxLength = uint.MaxValue;
            OnEnter = delegate () { };
            OnClick = StartTyping;
        }

        public void StartTyping()
        {
            Globals.EventManager.AddEvent(TypingRoutine, "TypingRoutine", 0, 0, 0);
        }

        private int TypingRoutine()
        {
            if(Text.Length < MaxLength)
                Text += InputManager.Keyboard.GetStringTyped();
            if (Text.Length > 0 && InputManager.Keyboard.KeyTyped(Microsoft.Xna.Framework.Input.Keys.Back))
                Text = Text.Remove(Text.Length - 1);
            if (InputManager.Mouse.ButtonPushed(Mouse.MouseButtons.LeftButton))
                return 0;
            if (InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Enter))
            {
                OnEnter();
                return 0;
            }
            return 1;
        }
    }

    public enum MessageBoxType { OK, YesNo };
    public enum MessageBoxReturn { OK, YES, NO, Nothing };

    public class MessageBox : Window
    {
        public MessageBox(string text, MessageBoxType type = MessageBoxType.OK) : base(null,true)
        {
            Position = new Vector2(-7, 5);
            Size = new Vector2(14, 10);
            Color = new Color(1, 0, 0, 0.85f);
            Text = text;
            zText.VerticalAlignment = VerticalAlignment.Top;
            TextColor = Color.White;
            Globals.GuiManager.LastMessageBoxReturn = MessageBoxReturn.Nothing;

            if (type == MessageBoxType.OK)
                InitOk();
            else if (type == MessageBoxType.YesNo)
                InitYesno();
        }

        private void InitOk()
        {
            Window ok = new Window(this);
            ok.Size = new Vector2(4, 2);
            ok.Position = Position + new Vector2(5, -7);
            ok.Text = "OK";
            ok.CenterText();
            ok.OnClick = OkClick;
            ok.TextColor = Color.Black;
            var e = Globals.EventManager.AddEvent(Escape, "mbescape", 0, 0, 0, EventType.PreEvent);
            e.MoveToFront();
        }

        private void InitYesno()
        {
            Window yes = new Window(this);
            yes.Size = new Vector2(4, 2);
            yes.Position = Position + new Vector2(2, -7);
            yes.Text = "YES";
            yes.CenterText();
            yes.OnClick = YesClick;
            yes.TextColor = Color.Black;

            Window no = new Window(this);
            no.Size = new Vector2(4, 2);
            no.Position = Position + new Vector2(8, -7);
            no.Text = "NO";
            no.CenterText();
            no.OnClick = NoClick;
            no.TextColor = Color.Black;

            var e = Globals.EventManager.AddEvent(Escape, "mbescape", 0, 0, 0, EventType.PreEvent);
            e.MoveToFront();
        }

        public int Escape()
        {
            if (InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Escape))
            {
                InputManager.Keyboard.IgnoreKeyForOneFrame(Microsoft.Xna.Framework.Input.Keys.Escape);
                Globals.GuiManager.LastMessageBoxReturn = MessageBoxReturn.NO;
                Destroy();
                return 0;
            }
            return 1;
        }

        private void OkClick()
        {
            Globals.EventManager.RemoveEvent("mbescape");
            Globals.GuiManager.LastMessageBoxReturn = MessageBoxReturn.OK;
            Destroy();
        }

        private void YesClick()
        {
            Globals.EventManager.RemoveEvent("mbescape");
            Globals.GuiManager.LastMessageBoxReturn = MessageBoxReturn.YES;
            Destroy();
        }

        private void NoClick()
        {
            Globals.EventManager.RemoveEvent("mbescape");
            Globals.GuiManager.LastMessageBoxReturn = MessageBoxReturn.NO;
            Destroy();
        }

        public override void Destroy()
        {
            base.Destroy();
        }
    }
}

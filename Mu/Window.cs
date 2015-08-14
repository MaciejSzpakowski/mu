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

namespace Mu
{
    public delegate void VoidFunction();

    public class Window
    {
        public string Name; //to find the window easly
        protected Text mText;
        protected Sprite mSprite;
        protected bool mVisible;
        public VoidFunction OnClick;
        public Window mParent;
        protected Layer mLayer;
        public List<Window> mChildren;
        protected bool mReceivedButtonDown;
        protected Vector2 mOrigin; //i need this crap to maintain correct offset from origin
        public Sprite Sprite { get { return mSprite; } }

        public Window(Window owner = null)
        {
            Name = string.Empty;
            mOrigin = new Vector2(0, 0);
            mReceivedButtonDown = false;
            mSprite = SpriteManager.AddSprite(IOPath.Combine(Path.Texture, "pixel.bmp"));
            mSprite.ColorOperation = ColorOperation.ColorTextureAlpha;
            Color = Color.White;
            mVisible = true;
            OnClick = delegate () { };
            mParent = owner;
            mChildren = new List<Window>();
            mText = TextManager.AddText(string.Empty, Globals.Font);
            mText.ColorOperation = ColorOperation.ColorTextureAlpha;
            mText.AttachTo(mSprite, false);
            if (owner == null)
                InitializeAsTopLevel();
            else
                InitializeAsChild();
            SpriteManager.AddToLayer(mSprite, mLayer);
            TextManager.AddToLayer(mText, mLayer);
            Size = new Vector2(1, 1);
        }

        /// <summary>
        /// Returns true if this.Layer is higer than w.Layer
        /// </summary>
        /// <param name="w">window to compare to</param>
        /// <returns></returns>
        public bool IsHigherThan(Window w)
        {
            return SpriteManager.Layers.IndexOf(mLayer) > SpriteManager.Layers.IndexOf(w.mLayer);
        }

        public void MoveToFront()
        {
            SpriteManager.MoveToFront(mLayer);
        }

        public void mClearReceivedClick()
        {
            mReceivedButtonDown = false;
        }

        public void Activity()
        {
            if (!mVisible)
                return;
            foreach (Window w in mChildren)
                    w.Activity();
        }

        protected void InitializeAsChild()
        {
            mLayer = mParent.mLayer;            
            mParent.mChildren.Add(this);
            mSprite.Position.Z = mParent.mSprite.Position.Z + 0.001f;
        }

        protected void InitializeAsTopLevel()
        {
            mLayer = SpriteManager.AddLayer();
        }        

        public Vector2 Position
        {
            get { return mOrigin; }
            set
            {
                Vector2 diff = value - mOrigin;
                mOrigin = value;
                Sprite.Position = new Vector3(mOrigin.X + mSprite.ScaleX,
                    mOrigin.Y - mSprite.ScaleY, mSprite.Position.Z);
                foreach (Window w in mChildren)
                    w.Position += diff;
            }
        }

        public Color Color
        {
            get { return new Color(mSprite.Red,mSprite.Green,mSprite.Blue,mSprite.Alpha); }
            set { mSprite.Red = value.R; mSprite.Green = value.G; mSprite.Blue = value.B; mSprite.Alpha = value.A; }
        }

        public Vector2 Size
        {
            get { return new Vector2(mSprite.ScaleX*2, mSprite.ScaleY*2); }
            set
            {
                value.X /= 2;
                value.Y /= 2;
                mSprite.ScaleX = value.X;
                mSprite.ScaleY = value.Y;
                //realign, position changes which is important to display window correctly
                Position = Position;
                //realing text since now windows looks different
                mText.RelativePosition = new Vector3(-mSprite.ScaleX, mSprite.ScaleY - mText.Height / 2, 0.001f);
            }
        }

        public bool Visible
        {
            get { return mVisible; }
            set
            {
                mVisible = value;
                if (value == false)
                    foreach (Window w in mChildren)
                        w.Hide();
                else
                    foreach (Window w in mChildren)
                        w.Show();
            }
        }

        public float Transparency
        {
            get { return mSprite.Alpha; }
            set { mSprite.Alpha = value; }
        }

        /// <summary>
        /// Recursive show
        /// </summary>
        public void Show()
        {
            mVisible = true;
            foreach (Window w in mChildren)
                w.Show();
        }

        /// <summary>
        /// Recursive hide
        /// </summary>
        public void Hide()
        {
            mVisible = false;
            foreach (Window w in mChildren)
                w.Hide();
        }

        public string Text
        {
            get{ return mText.DisplayText; }
            set{ mText.DisplayText = value;}
        }

        public Color TextColor
        {
            get { return mText.GetColor(); }
            set { mText.SetColor(value.R,value.G,value.B); mText.Alpha = value.A; }
        }

        /// <summary>
        /// Do stuff like:
        /// call OnClick if window cliked, make it focued, drag etc.
        /// used only by GuiManager
        /// m in front of he name added to obscure
        /// returns wheter window is being dragged or not
        /// </summary>
        public bool mExecuteMouseRoutine()
        {
            if (InputManager.Mouse.ButtonPushed(Mouse.MouseButtons.LeftButton))
                Focus();
            if (mParent == null && mReceivedButtonDown && InputManager.Mouse.ButtonDown(Mouse.MouseButtons.LeftButton))
                Drag();
            if (InputManager.Mouse.ButtonReleased(Mouse.MouseButtons.LeftButton))
                ButtonReleased();            
            if (mParent != null)
                return false;
            else
                return mReceivedButtonDown;
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
            mReceivedButtonDown = true;
            Globals.GuiManager.mSetWindowReceivedClick(this);
        }

        /// <summary>
        /// execute onclick if same window received lmb down lmb up (click)
        /// </summary>
        protected void ButtonReleased()
        {
            if(mReceivedButtonDown)
                OnClick();
            mReceivedButtonDown = false;
        }

        /// <summary>
        /// This should be called on top level window, it will clean all children
        /// </summary>
        public void Destroy()
        {
            if (mParent != null)
                throw new AccessViolationException("Window.Destroy() can be called on top level window only");
            SpriteManager.RemoveSprite(mSprite);
            TextManager.RemoveText(mText);
            foreach (Window w in mChildren)
                w.DestroyChild();
            SpriteManager.RemoveLayer(mLayer);
            mChildren.Clear();
            Globals.GuiManager.RemoveWindow(this);

        }

        /// <summary>
        /// This should be called only when editing content of a window
        /// </summary>
        public void DestroyChild()
        {
            if (mParent == null)
                throw new AccessViolationException("Window.DestroyChild() can be called on child window only");

            SpriteManager.RemoveSprite(mSprite);
            TextManager.RemoveText(mText);
            foreach (Window w in mChildren)
                w.DestroyChild();
            mChildren.Clear();
        }
    }

    public class TextBox : Window
    {
        public VoidFunction OnEnter;
        private bool mTyping;

        public TextBox(Window owner) : base(owner)
        {
            OnEnter = delegate () { };
        }
    }
}

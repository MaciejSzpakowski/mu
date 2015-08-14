using FlatRedBall.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mu
{
    public class MuGuiManager
    {
        //top level only
        List<Window> mWindows;
        //all windows can be here
        Window mWindowCursorOwner; //<< window that will execute mouse routine
        Window mWindowReceivedClick; //<< window that received click, need to store it so it can be cleared properly
        bool mWindowIsDragged;

        public MuGuiManager()
        {
            mWindowReceivedClick = null;
            mWindowIsDragged = false;
            mWindows = new List<Window>();
            mWindowCursorOwner = null;
        }

        public void AddWindow(Window w)
        {
            mWindows.Add(w);
        }

        public void mSetWindowReceivedClick(Window w)
        {
            mWindowReceivedClick = w;
        }

        public void RemoveWindow(Window w)
        {
            mWindows.Remove(w);
        }

        public void Activity()
        {
            //doesnt make sense to check for any under cursor staff when
            //some windows is dragged
            if (!mWindowIsDragged)
            {
                GetWindowsUnderCursor();
                Functions.UnclipCursor();
            }
            else
                Functions.ClipCursorInGameWindow();
            if (mWindowCursorOwner != null)
                mWindowIsDragged = mWindowCursorOwner.mExecuteMouseRoutine();
            foreach (Window w in mWindows)
                w.Activity();
        }

        private bool IsWindowUnderCursor(Window w)
        {
            return w.Visible && GuiManager.Cursor.IsOn(w.Sprite);
        }

        private void RecursiveGetWindowUnderCur(List<Window> win)
        {
            foreach (Window w in win)
                if (IsWindowUnderCursor(w))
                {
                    ShouldReceiveClick(w);
                    RecursiveGetWindowUnderCur(w.mChildren);
                }
        }

        private void GetWindowsUnderCursor()
        {
            mWindowCursorOwner = null;
            RecursiveGetWindowUnderCur(mWindows);
            if (mWindowReceivedClick != null && mWindowReceivedClick != mWindowCursorOwner)
            {
                mWindowReceivedClick.mClearReceivedClick();
                mWindowReceivedClick = null;
            }
        }

        /// <summary>
        /// Decides  wheter currently detected window under cursor should
        /// me executing clicking routines
        /// </summary>
        /// <param name="w"></param>
        private void ShouldReceiveClick(Window w)
        {
            if (mWindowCursorOwner == null)
                mWindowCursorOwner = w;
            else if (w.mParent == mWindowCursorOwner)
                mWindowCursorOwner = w;
            else if (w.IsHigherThan(mWindowCursorOwner))
                mWindowCursorOwner = w;

        }

        public void Clear()
        {
            while (mWindows.Count > 0)
                mWindows.Last().Destroy();
        }
    }
}

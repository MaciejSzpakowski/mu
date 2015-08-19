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
        private List<Window> zWindows;
        //all windows can be here
        private Window zWindowCursorOwner; //<< window that will execute mouse routine
        private Window zLastCursorOwner; //used to execute mouseenter and mouseleave
        public bool zDoGetWindows;
        public MessageBoxReturn LastMessageBoxReturn;
        private List<Window> zModalWindows;
        public string LastMbMessage;

        public MuGuiManager()
        {
            LastMbMessage = string.Empty;
            zModalWindows = new List<Window>();
            zDoGetWindows = true;
            zWindows = new List<Window>();
            zLastCursorOwner = null;
            zWindowCursorOwner = null;
            LastMessageBoxReturn = MessageBoxReturn.Nothing;
        }

        public void AddWindow(Window w)
        {
            zWindows.Add(w);
        }

        public void RemoveWindow(Window w)
        {
            zWindows.Remove(w);
        }

        public void Activity()
        {
            //for modal window, run this routine only for the last one
            if (zModalWindows.Count > 0)
            {
                List<Window> list = new List<Window>(1);
                list.Add(zModalWindows.Last());
                RecursiveGetWindowUnderCur(list);
            }
            else if (zDoGetWindows)
                RecursiveGetWindowUnderCur(zWindows);
            MouseEnterLeave();
            if (zWindowCursorOwner != null)
            {                
                zWindowCursorOwner.ExecuteMouseRoutine();
                zWindowCursorOwner = null;
            }
        }

        public bool GetMbResult(MessageBoxReturn result, string msg)
        {
            return result == LastMessageBoxReturn && msg == LastMbMessage;
        }

        private void MouseEnterLeave()
        {
            if (zLastCursorOwner != zWindowCursorOwner)
            {
                if (zLastCursorOwner != null)
                    zLastCursorOwner.MouseLeave();
                if (zWindowCursorOwner != null)
                    zWindowCursorOwner.MouseEnter();
            }
            zLastCursorOwner = zWindowCursorOwner;
        }

        private void RecursiveGetWindowUnderCur(List<Window> win)
        {
            foreach (Window w in win)
                if (w.IsUnderCursor())
                {
                    ShouldReceiveClick(w);
                    RecursiveGetWindowUnderCur(w.zChildren);
                }
        }        

        /// <summary>
        /// Decides  wheter currently detected window under cursor should
        /// me executing clicking routines
        /// </summary>
        /// <param name="w"></param>
        private void ShouldReceiveClick(Window w)
        {
            if (zWindowCursorOwner == null)
                zWindowCursorOwner = w;
            else if (w.zParent == zWindowCursorOwner)
                zWindowCursorOwner = w;
            else if (w.IsHigherThan(zWindowCursorOwner))
                zWindowCursorOwner = w;

        }

        public void AddModalWindow(Window w)
        {
            zModalWindows.Add(w);
        }

        public void RemoveModalWindow(Window w)
        {
            zModalWindows.Remove(w);
        }

        public void Clear()
        {
            LastMessageBoxReturn = MessageBoxReturn.Nothing;
            LastMbMessage = string.Empty;
            while (zWindows.Count > 0)
                zWindows.Last().Destroy();
            while (zModalWindows.Count > 0)
                zModalWindows.Last().Destroy();
        }
    }
}

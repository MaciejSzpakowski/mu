﻿using FlatRedBall.Gui;
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
        public bool zDoGetWindows;
        public MessageBoxReturn LastMessageBoxReturn;
        private List<Window> zModalWindows;

        public MuGuiManager()
        {
            zModalWindows = new List<Window>();
            zDoGetWindows = true;
            zWindows = new List<Window>();
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
            Globals.Debug(zWindows.Count, "Windows");
            Globals.Debug(zModalWindows.Count, "modals");
            //for modal window, run routine only for the last one
            if (zModalWindows.Count > 0)
            {
                List<Window> list = new List<Window>(1);
                list.Add(zModalWindows.Last());
                RecursiveGetWindowUnderCur(list);
            }
            else if (zDoGetWindows)
                RecursiveGetWindowUnderCur(zWindows);
            if (zWindowCursorOwner != null)
            {
                zWindowCursorOwner.ExecuteMouseRoutine();
                zWindowCursorOwner = null;
            }
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
            while (zWindows.Count > 0)
                zWindows.Last().Destroy();
            while (zModalWindows.Count > 0)
                zModalWindows.Last().Destroy();
        }
    }
}

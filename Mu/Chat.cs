using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Mu
{
    public struct ChatMessage
    {
        string text;
        Color foreground;
        Color background;
        public ChatMessage(string s, Color f, Color b)
        {
            text = s;
            foreground = f;
            background = b;
        }
    }

    public class Chat : Window
    {
        private int zMaxCount;
        private int zShowCount;
        List<Window> zTexts;
        Queue<ChatMessage> zMessages;

        public Chat() : base()
        {
            InitProps(new Vector2(-10,0),new Vector2(10,10), new Color(0.3f, 0.3f, 0.3f, 0.75f), "", Color.White);
            zMaxCount = 100;
            zShowCount = 7;
            zTexts = new List<Window>();
            for (int i = 0; i < zShowCount; i++)
            {
                Window w = new Window(this);
                w.InitProps(Position + new Vector2(0, (-zShowCount + i) * 1.5f), 
                    new Vector2(15, 1), new Color(0.1f, 0.1f, 0.1f, 1), "", Color.White);
                w.Visible = false;
            }
            zMessages = new Queue<ChatMessage>();
        }

        public void Post(string text, Color foreground, Color background)
        {
            zMessages.Enqueue(new ChatMessage(text, foreground, background));
            if (zMessages.Count > zMaxCount)
                zMessages.Dequeue();
            RefreshChar();
        }

        private void RefreshChar()
        {
            for (int i = 0; i < zShowCount && i < zMessages.Count; i++)
                SetMessageToWindow(zTexts, zMessages.ElementAt(i));
        }
    }
}

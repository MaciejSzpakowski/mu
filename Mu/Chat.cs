using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using FlatRedBall.Input;

namespace Mu
{
    public struct ChatMessage
    {
        public string text;
        public Color foreground;
        public Color background;
        public ChatMessage(string s, Color f, Color b)
        {
            text = s;
            foreground = f;
            background = b;
        }
    }

    public class Chat : Window
    {
        private int zStartIndex;
        private int zMaxCount;
        private int zShowCount;
        private List<Window> zTexts;
        private Queue<ChatMessage> zMessages;
        public TextBox zMessageTextBox;
        public TextBox zWhisperTextBox;

        public Chat() : base()
        {
            InitProps(new Vector2(-22,-11),new Vector2(10,1), new Color(0.3f, 0.3f, 0.3f, 0.75f), "", Color.White);
            zMaxCount = 100;
            zShowCount = 7;
            zTexts = new List<Window>();
            for (int i = 0; i < zShowCount; i++)
            {
                Window w = new Window(this);
                w.InitProps(Position + new Vector2(0, i * 1.5f +2), 
                    new Vector2(15, 1), new Color(0.1f, 0.1f, 0.1f, 1), "", Color.White);
                w.Visible = false;
                zTexts.Add(w);
            }
            zMessages = new Queue<ChatMessage>();
            zStartIndex = -1;
            InitEvents();
            zMessageTextBox = new TextBox(this);
            zMessageTextBox.InitProps(Position + new Vector2(3, 0), new Vector2(10, 1), new Color(0.1f, 0.1f, 0.1f, 1), "", Color.White);
            zMessageTextBox.OnEnter = delegate ()
            {
                ProcessInput(zMessageTextBox.Text);
                zMessageTextBox.Text = string.Empty;
            };
        }

        private void ProcessInput(string text)
        {
            Post(Globals.Players[0].Name+": " + text, Color.White, Color.Black);
            Globals.Client.SendChat(text, 0);
        }

        private void InitEvents()
        {
            Globals.EventManager.AddEvent(delegate ()
            {
                Debug.Print(zStartIndex,"start index");
                if (zCollect)
                    return 0;
                if (zMessages.Count == 0)
                    return 1;
                if (InputManager.Keyboard.KeyTyped(Microsoft.Xna.Framework.Input.Keys.PageDown))
                {
                    zStartIndex++;
                }
                else if (InputManager.Keyboard.KeyTyped(Microsoft.Xna.Framework.Input.Keys.PageUp))
                {
                    zStartIndex--;
                }
                else
                    return 1;
                zStartIndex = zStartIndex.Clamp(0, zMessages.Count - 1);
                RefreshChat();                
                return 1;
            }, "chatscroll");
        }

        public void Post(string text, Color foreground, Color background)
        {
            if (text == string.Empty)
                return;
            zStartIndex++;
            zMessages.Enqueue(new ChatMessage(text, foreground, background));
            if (zMessages.Count > zMaxCount)
                zMessages.Dequeue();
            RefreshChat();
        }

        private void RefreshChat()
        {
            //iterate over back of th queue
            for (int i = 0, j=zStartIndex; i < zShowCount; i++,j--)
            {
                if (j >= 0)
                    SetMessageToWindow(zTexts[i], zMessages.ElementAt(j));
                else
                    zTexts[i].Visible = false;
            }
        }

        private void SetMessageToWindow(Window w, ChatMessage chatMessage)
        {
            float barHeight = 1;
            w.Text = chatMessage.text;
            w.Color = chatMessage.background;
            w.TextColor = chatMessage.foreground;
            w.Visible = true;
            w.Size = new Vector2(w.zText.Width + 0.4f, barHeight);
        }
    }
}

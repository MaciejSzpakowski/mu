using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FlatRedBall.Screens;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;

namespace Mu
{
    public delegate void ExecuteCommand(string[] tokens);

    public partial class Console : Form
    {
        List<ValidCommand> mCommands;

        public class ValidCommand
        {
            public string Cmd;
            public bool Match(string cmd)
            {
                return cmd == Cmd;
            }

            public ExecuteCommand ExecuteCommand;
        }

        public Console()
        {
            InitializeComponent();
        }

        private void textBoxInput_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (textBoxInput.Text == string.Empty)
                    return;
                ProcessCommand(textBoxInput.Text);
                textBoxInput.Text = string.Empty;
            }
        }

        void ProcessCommand(string cmd)
        {
            if (cmd == string.Empty)
                throw new ArgumentException("Processed arg cannot be empty");
            string[] tokens = cmd.Split(' ');
            ValidCommand vc = mCommands.FirstOrDefault(com => com.Match(tokens[0]));
            if (vc == null)
                Write("Invalid command");
            else
                vc.ExecuteCommand(tokens);
        }

        /// <summary>
        /// Use this to add a new command
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="executeCommand"></param>
        void AddCommand(string cmd, ExecuteCommand executeCommand)
        {
            var newCmd = new ValidCommand();
            newCmd.Cmd = cmd;
            newCmd.ExecuteCommand = executeCommand;
            mCommands.Add(newCmd);
        }

        /// <summary>
        /// Write line to console
        /// </summary>
        /// <param name="text"></param>
        public void Write(string text)
        {
            textBoxOutput.AppendText(text + "\n");
        }

        private void Console_Load(object sender, EventArgs e)
        {
            mCommands = new List<ValidCommand>();
            Write("Console started");
            // Add commands HERE:
            //help
            AddCommand("help", delegate (string[] tokens)
            {
                string output = "List of all commands:";
                foreach (ValidCommand vc in mCommands)
                {
                    output += vc.Cmd;
                    if (vc != mCommands.Last())
                        output += ',';
                }
                Write(output);
            });
            //test
            AddCommand("test", delegate (string[] tokens)
            {
            });
            //exit
            AddCommand("exit", delegate (string[] tokens)
            {
                Write("Exiting...");
                Application.Exit();
            });
            //hide
            AddCommand("hide", delegate (string[] tokens)
            {
                if (tokens.Length > 1 && tokens[1] == "printdebug")
                    Debug.PrintDebug.Hide();
            });
            //show
            AddCommand("show", delegate (string[] tokens)
            {
                if (tokens.Length > 1 && tokens[1] == "printdebug")
                    Debug.PrintDebug.Show();
            });
            //send msg from client to server
            AddCommand("send", delegate (string[] tokens)
            {
                if (tokens.Length <= 1 || Globals.Client == null)
                    return;
                Globals.Client.SendMessage(MsgHeader.Null, tokens[1]);
                Write("Message sent");
            });
            //disconnect
            AddCommand("disconnect", delegate (string[] tokens)
            {
                if (Globals.Client == null)
                    Debug.Write("Client doesnt exist");
                else
                    Globals.Client.Disconnect(false);
            });
            //stop
            AddCommand("stop", delegate (string[] tokens)
            {
                if (Globals.Server == null)
                    Debug.Write("Sever doesnt exist");
                else
                    Globals.Server.Stop();
            });
            //start
            AddCommand("start", delegate (string[] tokens)
            {
                if (Globals.Server == null)
                    Debug.Write("Sever doesnt exist");
                if (tokens.Length < 2)
                    Debug.Write("Not enough parametes");
                else
                {
                    int port = 0;
                    int.TryParse(tokens[1], out port);
                    if (port == 0)
                        Debug.Write("Wrong port");
                    else
                    {
                        Globals.Server.Start(port);
                        Globals.Server.StartAccepting();
                    }
                }
            });
            //connect
            AddCommand("connect", delegate (string[] tokens)
            {
                if (Globals.Client == null)
                    Debug.Write("Client doesnt exist");
                if (tokens.Length < 3)
                    Debug.Write("Not enough parametes");
                else
                {
                    int port = 0;
                    int.TryParse(tokens[2], out port);
                    if (port == 0)
                        Debug.Write("Wrong port");
                    else
                        Globals.Client.Connect(tokens[1], port);
                }
            });
            //post to chat
            AddCommand("post", delegate (string[] tokens)
            {
                if (tokens.Length <= 1)
                    return;
                string post = "";
                for (int i = 1; i < tokens.Length; i++)
                    post += tokens[i] + " ";
                Globals.Chat.Say(Globals.Players[0].Name, post);
            });
            //add mob
            AddCommand("spawnmob", delegate (string[] toknes)
            {
                for (int i = 0; i < 6; i++)
                {
                    Mob m = Globals.Server.SpawnMob(MobClass.BudgeDragon, MobMap.Lorencia);
                }
            });
        }
    }
}

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mu
{
    public partial class Server
    {
        private void ProcessMessage(Message msg, ServerClient c)
        {
            byte[] data = msg.Data;
            byte header = data[0];

            switch (header)
            {
                case MsgHeader.ClientReady:
                    InitNewPlayer(c, data);
                    break;
                case MsgHeader.PlayerPos:
                    UpdatePlayerPos(c, data);
                    break;
                default:
                    throw new ArgumentException("Unhandled message received by server");
            }
        }

        private void UpdatePlayerPos(ServerClient c, byte[] rawdata)
        {
            object[] data = Functions.GetData(rawdata, "ff");
            c.Hero.Position.X = (float)data[0];
            c.Hero.Position.Y = (float)data[0];
            RelayMessage(c, rawdata);
        }

        /// <summary>
        /// Appends netid and resend to everyone else than c
        /// </summary>
        /// <param name="c"></param>
        /// <param name="rawdata"></param>
        private void RelayMessage(ServerClient c, byte[] rawdata)
        {
            byte[] relaymsg = new byte[rawdata.Length + 5];
            relaymsg[0] = (byte)(rawdata.Length + 4);
            Array.Copy(rawdata, 0, relaymsg, 1, rawdata.Length);
            Array.Copy(c.zNetidBytes, 0, relaymsg, rawdata.Length + 1, 4);
            for (int i = 0; i < zClients.Count; i++)
                if (zClients[i] != c)
                    zClients[i].zSendMessage(relaymsg);
        }

        /// <summary>
        /// Sends newly connected client data about players, monsters and items on the ground
        /// </summary>
        /// <param name="c"></param>
        private void InitNewPlayer(ServerClient client, byte[] rawData)
        {
            object[] data = Functions.GetData(rawData, "sc");
            client.Hero.Name = data[0] as string;
            client.Hero.Class = (HeroClass)Convert.ToChar(data[1]);
            client.Hero.Position = new Vector3(0, 0, 0);
            //tell old players about new player and new player about old players
            foreach (var c in zClients.Where(c1 => c1 != client))
            {
                client.SendAddPlayer(c.Hero);
                c.SendAddPlayer(client.Hero);
            }
        }
    }

    public partial class Client : ServerClient
    {
        private void ProcessMessage(Message msg)
        {
            byte[] data = msg.Data;
            byte header = data[0];

            switch (header)
            {
                case MsgHeader.Welcome:
                    Globals.Client.zReceivedWelcomeMessage = true;
                    break;
                case MsgHeader.AddPlayer:
                    AddPlayer(data);
                    break;
                case MsgHeader.RemovePlayer:
                    RemovePlayer(data);
                    break;
                case MsgHeader.PlayerPos:
                    UpdatePlayerPos(data);
                    break;
                default:
                    throw new ArgumentException("Unhandled message received by client");
            }
        }

        private void UpdatePlayerPos(byte[] rawdata)
        {
            object[] data = Functions.GetData(rawdata, "ffi");
            var h = Globals.Players.First(h1 => h1.Netid == (int)data[2]);
            h.X = (float)data[0];
            h.Y = (float)data[1];
        }

        private void RemovePlayer(byte[] rawdata)
        {
            object[] data = Functions.GetData(rawdata, "i");
            var h = Globals.Players.First(h1 => h1.Netid == (int)data[0]);
            h.Destroy();
            Globals.Players.Remove(h);
        }

        private void AddPlayer(byte[] rawdata)
        {
            object[] data = Functions.GetData(rawdata, "sicff");
            Hero h = new Hero((string)data[0], (HeroClass)Convert.ToChar(data[2]));
            h.Netid = (int)data[1];
            h.Position = new Vector3((float)data[3], (float)data[4], ZLayer.Npc);
            Globals.Players.Add(h);
        }

        //messages send by client
        public void SendReady()
        {
            Hero h = Globals.Players[0];
            SendMessage(MsgHeader.ClientReady, h.Name, (char)h.Class);
        }

        public void SendPos(float x,float y)
        {
            Hero h = Globals.Players[0];
            SendMessage(MsgHeader.PlayerPos, x, y);
        }
    }
    
    public partial class ServerClient
    {
        //messages send by server
        public void SendAddPlayer(ClientHero h)
        {
            SendMessage(MsgHeader.AddPlayer, h.Name, h.Netid, (char)h.Class, h.Position.X, h.Position.Y);
        }

        public void SendRemovePlayer(ServerClient c)
        {
            SendMessage(MsgHeader.RemovePlayer, c.Hero.Netid);
        }
    }

    public static class MsgHeader
    {
        public const byte Welcome = 1;
        public const byte ClientReady = 2;
        public const byte AddPlayer = 3;
        public const byte RemovePlayer = 4;
        public const byte PlayerPos = 5;
    }
}

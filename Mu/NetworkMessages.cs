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
            if (Debug.DebugMode && data[0] != MsgHeader.PlayerPos)
                PrintMsg(data);

            switch (header)
            {
                case MsgHeader.Null:
                    break;
                case MsgHeader.ClientReady:
                    InitNewPlayer(c, data);
                    break;
                case MsgHeader.PlayerPos:
                    UpdatePlayerPos(c, data);
                    break;
                case MsgHeader.Chat:
                    RelayMessage(c, data);
                    break;
                default:
                    throw new ArgumentException("Unhandled message received by server");
            }
        }

        private void PrintMsg(byte[] message)
        {
            string str = "from client";
            foreach (byte b in message)
                str += $"[{b}]";
            Debug.Write(str);
        }

        private void UpdatePlayerPos(ServerClient c, byte[] rawdata)
        {
            var data = Functions.GetData(rawdata);
            c.Hero.Position.X = (float)data[0];
            c.Hero.Position.Y = (float)data[1];
            RelayMessage(c, rawdata);
        }

        /// <summary>
        /// Appends netid and resend to everyone else than c
        /// </summary>
        /// <param name="c">who was sender originally</param>
        /// <param name="rawdata">this is 'data' array from 'Server.ProcessMessage()'</param>
        private void RelayMessage(ServerClient c, byte[] rawdata)
        {
            //+6 means 1 byte in the fron for len, 1 byte in the back for format and very last 4 bytes for int
            byte[] relaymsg = new byte[rawdata.Length + 6];
            //new length
            relaymsg[0] = (byte)(rawdata.Length + 5);
            Array.Copy(rawdata, 0, relaymsg, 1, rawdata.Length);
            //add netid
            relaymsg[rawdata.Length + 1] = (byte)'i';
            Array.Copy(c.zNetidBytes, 0, relaymsg, rawdata.Length + 2, 4);
            //resend
            for (int i = 0; i < Clients.Count; i++)
                if (Clients[i] != c)
                    Clients[i].zSendMessage(relaymsg);
        }

        /// <summary>
        /// Sends newly connected client data about players, monsters and items on the ground
        /// </summary>
        /// <param name="c"></param>
        private void InitNewPlayer(ServerClient client, byte[] rawData)
        {
            var data = Functions.GetData(rawData);
            client.Hero.Name = data[0] as string;
            client.Hero.Class = (HeroClass)Convert.ToChar(data[1]);
            client.Hero.Position = new Vector3(0, 0, 0);
            //tell old players about new player and new player about old players
            foreach (var c in Clients.Where(c1 => c1 != client))
            {
                client.SendAddPlayer(c.Hero);
                c.SendAddPlayer(client.Hero);
            }
            foreach (Mob m in Mobs)
                client.SendNewMob(m);
        }        
    }

    public partial class Client : ServerClient
    {
        private Hero GetHeroFromNetid(int netid) => Globals.Players.First(h1 => h1.Netid == netid);
        private Mob GetMobFromNetid(int netid) => Mobs.First(mob => mob.Netid == netid);

        private void ProcessMessage(Message msg)
        {
            byte[] data = msg.Data;
            byte header = data[0];
            if (Debug.DebugMode && data[0] != MsgHeader.PlayerPos)
                PrintMsg(data);

            switch (header)
            {
                case MsgHeader.Null:
                    break;
                case MsgHeader.Welcome:
                    Globals.Client.ReceivedWelcomeMessage = true;
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
                case MsgHeader.Chat:
                    PostChat(data);
                    break;
                case MsgHeader.Newmob:
                    AddMob(data);
                    break;
                default:
                    throw new ArgumentException("Unhandled message received by client");
            }
        }

        private void AddMob(byte[] rawdata)
        {
            var data = Functions.GetData(rawdata);
            Mob m = Mob.MobClient((MobClass)data[0]);
            m.Netid = (int)data[1];
            m.Position = new Vector3((float)data[2], (float)data[3], ZLayer.Npc);
            Mobs.Add(m);
        }        

        private void PostChat(byte[] rawdata)
        {
            var data = Functions.GetData(rawdata);
            var h = GetHeroFromNetid((int)data[2]);
            Globals.Chat.Say(h.Name, (string)data[0]);
        }

        private void PrintMsg(byte[] message)
        {
            string str = "from server";
            foreach (byte b in message)
                str += $"[{b}]";
            Debug.Write(str);
        }

        private void UpdatePlayerPos(byte[] rawdata)
        {
            var data = Functions.GetData(rawdata);
            var h = GetHeroFromNetid((int)data[2]);
            h.Target.X = (float)data[0];
            h.Target.Y = (float)data[1];
        }

        private void RemovePlayer(byte[] rawdata)
        {
            var data = Functions.GetData(rawdata);
            var h = GetHeroFromNetid((int)data[0]);
            h.Destroy();
            Globals.Players.Remove(h);
        }

        private void AddPlayer(byte[] rawdata)
        {
            var data = Functions.GetData(rawdata);
            Hero h = new Hero((string)data[0], (HeroClass)Convert.ToChar(data[2]));
            h.Netid = (int)data[1];
            h.Position = new Vector3((float)data[3], (float)data[4], ZLayer.Npc);
            h.Target = h.Position;
            Globals.Players.Add(h);
        }

        //messages send by client
        /// <summary>
        /// Informs server that this player loaded the map and its ready to load players, mobs and items
        /// </summary>
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

        /// <summary>
        /// Send chat message
        /// </summary>
        /// <param name="text"></param>
        /// <param name="flags">Extra info, might be used to display different color on the other side</param>
        public void SendChat(string text, byte flags)
        {
            SendMessage(MsgHeader.Chat, text, flags);
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

        public void SendNewMob(Mob m)
        {
            SendMessage(MsgHeader.Newmob, (int)m.Class, m.Netid, m.Position.X, m.Position.Y);
        }
    }

    public static class MsgHeader
    {
        public const byte Null = 0;
        public const byte Welcome = 1;
        public const byte ClientReady = 2;
        public const byte AddPlayer = 3;
        public const byte RemovePlayer = 4;
        public const byte PlayerPos = 5;
        public const byte Chat = 6;
        public const byte Newmob = 7;
    }
}

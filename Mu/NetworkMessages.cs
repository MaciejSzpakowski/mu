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
                    RelayMessage(c, data, RelaySwitch.All);
                    break;
                case MsgHeader.Quit: //tell everyone in the room to disconnect
                    foreach (var c1 in c.Room.Clients.Where(c2 => c2 != c))
                        c1.SendRemovePlayer(c);
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
            RelayMessage(c, rawdata, RelaySwitch.Room);
        }

        public enum RelaySwitch { Room, All };
        /// <summary>
        /// Appends netid and resend to everyone else than c
        /// </summary>
        /// <param name="c">who was sender originally</param>
        /// <param name="rawdata">this is 'data' array from 'Server.ProcessMessage()'</param>
        private void RelayMessage(ServerClient c, byte[] rawdata, RelaySwitch rswitch)
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
            if (rswitch == RelaySwitch.Room)
            {
                foreach( ServerClient c1 in c.Room.Clients.Where(c2=>c2 != c))
                    c1.zSendMessage(relaymsg);
            }
            else
            {
                foreach (MapRoom r in Rooms.Values)
                    foreach (ServerClient client in r.Clients.Where(p => p != c))
                        client.zSendMessage(relaymsg);
            }
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
            MobMap heroMap = (MobMap)Convert.ToChar(data[2]);
            TransferClient(client, heroMap);
            client.Hero.Position = new Vector3(0, 0, 0);
            //tell old players about new player and new player about old players
            foreach (var c in client.Room.Clients.Where(c1 => c1 != client))
            {
                client.SendAddPlayer(c.Hero);
                c.SendAddPlayer(client.Hero);
            }
            //send mob data
            foreach (Mob m in client.Room.Mobs)
                client.SendNewMob(m);
        }

        private void TransferClient(ServerClient c, MobMap dst)
        {            
            ClientMutex.WaitOne();
            c.Room.Clients.Remove(c);
            c.Room = Rooms[dst];
            Rooms[dst].Clients.Add(c);
            ClientMutex.ReleaseMutex();
        }

        /// <summary>
        /// Update mob pos (target)
        /// </summary>
        /// <param name="m"></param>
        public void SendMobTarget(Mob m)
        {
            foreach (ServerClient c in Rooms[m.Map].Clients)
                c.SendMobTarget(m);
        }
    }

    public partial class Client : ServerClient
    {
        private Hero GetHeroFromNetid(int netid) => Globals.Players.First(h1 => h1.Netid == netid);
        private Mob GetMobFromNetid(int netid) => Mobs.First(mob => mob.Netid == netid);

        private void ProcessMessage(Message msg)
        {
            if (Transfering)
                return;
            byte[] data = msg.Data;
            byte header = data[0];
            if (Debug.DebugMode && data[0] != MsgHeader.PlayerPos && data[0] != MsgHeader.Mobpos)
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
                case MsgHeader.Mobpos:
                    UpdateMobPos(data);
                    break;
                default:
                    throw new ArgumentException("Unhandled message received by client");
            }
        }

        private void UpdateMobPos(byte[] rawdata)
        {
            var data = Functions.GetData(rawdata);
            Mob m = GetMobFromNetid((int)data[0]);
            m.Target = new Vector3((float)data[1], (float)data[2], ZLayer.Npc);
        }

        private void AddMob(byte[] rawdata)
        {
            var data = Functions.GetData(rawdata);
            Mob m = Mob.MobClient((MobClass)data[0], new Vector2((float)data[2], (float)data[3]));
            m.Netid = (int)data[1];
            m.Target = m.Position;            
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
            Transfering = false;
            Hero h = Globals.Players[0];
            SendMessage(MsgHeader.ClientReady, h.Name, (char)h.Class, (char)h.Map);
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

        public void SendQuit()
        {
            Transfering = true;
            SendMessage(MsgHeader.Quit);
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

        public void SendMobTarget(Mob m)
        {
            SendMessage(MsgHeader.Mobpos, m.Netid, m.Target.X, m.Target.Y);
        }

        public void SendHitPlayer(Mob m)
        {
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
        public const byte Mobpos = 8;
        public const byte Quit = 9;
        public const byte HitPlayer = 10;
    }
}

using FlatRedBall.Math;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Mu
{
    public enum ServerState { Running, Stopped };
    public enum ClientState { Connected, Disconnected };

    public class Message
    {
        public byte[] Data;
        public bool IsHeader(byte[] msg)
        {
            return msg[0] == Data[0];
        }
    }

    public class MapRoom
    {
        public MobMap Map;
        public List<ServerClient> Clients;
        public PositionedObjectList<Mob> Mobs;

        public MapRoom(MobMap map)
        {
            Map = map;
            Clients = new List<ServerClient>();
            Mobs = new PositionedObjectList<Mob>();
        }
    }

    public partial class Server
    {
        private ServerState State;
        private Mutex ClientMutex;
        private TcpListener Socket;        
        private Thread AcceptClientsThread;
        private int NextId;
        public int zUpload;
        private int Download;
        private int DisplayUp;
        private int DisplayDown;
        private Dictionary<MobMap,MapRoom> Rooms;

        public Server()
        {
            Rooms = new Dictionary<MobMap, MapRoom>();
            InsertAllRooms();
            DisplayUp = zUpload = 0;
            DisplayDown = Download = 0;
            NextId = 1;
            ClientMutex = new Mutex();
            Socket = null;
            State = ServerState.Stopped;
            AcceptClientsThread = new Thread(new ThreadStart(AcceptClients));
            AcceptClientsThread.IsBackground = true;
        }

        private void InsertAllRooms()
        {
            Rooms.Add(MobMap.Devias, new MapRoom(MobMap.Devias));
            Rooms.Add(MobMap.Dungeon, new MapRoom(MobMap.Dungeon));
            Rooms.Add(MobMap.Lorencia, new MapRoom(MobMap.Lorencia));
            Rooms.Add(MobMap.LostTower, new MapRoom(MobMap.LostTower));
            Rooms.Add(MobMap.Noria, new MapRoom(MobMap.Noria));
        }

        public bool Start(int port)
        {            
            try
            {
                Socket = new TcpListener(IPAddress.Any, port);
                Socket.Start();
            }
            catch (SocketException se)
            {
                //address/port occupied
                if (se.ErrorCode == 10048)
                    return false;
                throw se;
            }
            Globals.EventManager.AddEvent(UpdateTransfer, "updatetransfer", false, 0, 0, 1);
            State = ServerState.Running;
            Debug.Write("Server started");
            return true;
        }

        public Mob SpawnMob(MobClass mobclas, MobMap map)
        {
            Mob m = Mob.MobServer(mobclas,map);
            MapRoom room = Rooms[map];
            room.Mobs.Add(m);
            foreach (ServerClient c in room.Clients)
                c.SendNewMob(m);
            return m;
        }        

        private int UpdateTransfer()
        {
            if (State == ServerState.Stopped)
                return 0;
            DisplayDown = Download;
            DisplayUp = zUpload;
            Download = 0;
            zUpload = 0;
            return 1;
        }

        /// <summary>
        /// Starts a thread that accepts new clients
        /// </summary>
        public void StartAccepting()
        {
            Debug.Write("Server now accepting");
            AcceptClientsThread.Start();
        }

        public int GetNextId()
        {
            int result = NextId;
            NextId++;
            return NextId;
        }
        
        /// <summary>
        /// Stop accepting new clients (but dont stop server)
        /// </summary>
        public void StopAccepting()
        {
            Debug.Write("Server stopped accepting");
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks for disconnected clients
        /// </summary>
        public void Activity()
        {
            int clientsCount = 0;
            
            Debug.Print(DisplayUp, "Upload");
            Debug.Print(DisplayDown, "Download");
            foreach(MapRoom r in Rooms.Values)
            {
                for (int i = r.Clients.Count - 1; i >= 0; i--)
                {
                    clientsCount++;
                    ClientActivity(r.Clients[i]);
                }
                //mobs
                for (int i = r.Mobs.Count - 1; i >= 0; i--)
                    r.Mobs[i].ServerActivity();
            }
            Debug.Print(clientsCount, "Server client count");
        }

        private void ClientActivity(ServerClient c)
        {
            var msg = DequeueMessage(c);
            if (msg != null)
            {
                ProcessMessage(msg, c);
                Download += msg.Data.Length + 1;
            }
            if (c.ReceiveThread.ThreadState == ThreadState.Stopped)
            {
                ClientMutex.WaitOne();
                RemoveClient(c);
                ClientMutex.ReleaseMutex();
            }
        }

        public void RemoveClient(ServerClient c)
        {
            Debug.Write($"{c.IP} disconnected");
            foreach (var c1 in c.Room.Clients.Where(c2 => c2 != c))
                c1.SendRemovePlayer(c);
            c.Socket.Close();
            ClientMutex.WaitOne();
            c.Room.Clients.Remove(c);
            ClientMutex.ReleaseMutex();
        }

        public ServerState GetState() => State;

        /// <summary>
        /// Returns and removes 
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private Message DequeueMessage(ServerClient c) => c.mDequeueMessage();

        /// <summary>
        /// Closes all sockets, destroys all mobs and players.
        /// </summary>
        public void Stop()
        {
            Debug.Write("Server stopped");
            State = ServerState.Stopped;
            Socket.Stop();
            foreach (MapRoom r in Rooms.Values)
            {
                foreach (ServerClient c in r.Clients)
                {
                    c.Socket.Close();
                    while (r.Mobs.Count > 0)
                        r.Mobs.Last.Destroy();
                }
                r.Clients.Clear();
            }
        }

        public void RemoveMobs()
        {
            foreach(MapRoom r in Rooms.Values)
                foreach (Mob m in r.Mobs)
                    m.Remove();
        }

        /// <summary>
        /// Thread that accepts clients
        /// </summary>
        void AcceptClients()
        {
            try
            {
                while (true)
                {
                    var newSocket = Socket.AcceptTcpClient();
                    var newClient = new ServerClient(newSocket);
                    newClient.zId = GetNextId();
                    newClient.zNetidBytes = BitConverter.GetBytes(newClient.zId);
                    newClient.Hero.Netid = newClient.Id;
                    newClient.ReceiveThread.Start();
                    ClientMutex.WaitOne();
                    Rooms[MobMap.Lorencia].Clients.Add(newClient);
                    ClientMutex.ReleaseMutex();
                    //send welcome msg
                    newClient.SendMessage(MsgHeader.Welcome);
                }
            }
            catch (SocketException se)
            {
                if (se.ErrorCode == 10004 && State == ServerState.Stopped) { }
                else
                    throw se;
            }
        }
    }

    public partial class Client : ServerClient
    {
        private ClientState State;
        private bool ReceivedWelcomeMessage;
        private float WelcomeMessageTimeout;
        private PositionedObjectList<Mob> Mobs;        

        public Client() : base(null)
        {
            Mobs = new PositionedObjectList<Mob>();
            WelcomeMessageTimeout = 2;
        }

        public ClientState GetState() => State;
        
        /// <summary>
        /// Connect to server, returns true if connected succesfully
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public bool Connect(string host, int port)
        {
            zMessages.Clear();
            ReceivedWelcomeMessage = false;
            State = ClientState.Disconnected;
            zSocket = new TcpClient();
            try
            {
                zSocket.Connect(host, port);
                zReceiveThread = new Thread(new ThreadStart(Receive));
                zReceiveThread.IsBackground = true;
                zReceiveThread.Start();
            }
            catch (SocketException e)
            {
                //10061 usually means that server is not running or not accepting
                if (e.ErrorCode == 10061)
                    return false;
                //no response (timeout)
                if (e.ErrorCode == 10060)
                    return false;
                //server unknown
                if (e.ErrorCode == 11001)
                    return false;
                else
                    throw e;
            }
            Debug.Write("Client connected");
            State = ClientState.Connected;
            //wait for welcome message
            //its needed because in some cases client will report connected even
            //if it's not
            if (!GetWelcomeMsg())
            {
                Disconnect(false);
                return false;
            }
            return true;
        }

        private bool GetWelcomeMsg()
        {
            DateTime start = DateTime.Now;
            while (DateTime.Now - start < TimeSpan.FromSeconds(WelcomeMessageTimeout))
            {
                Activity();
                if (ReceivedWelcomeMessage)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public void Disconnect(bool showdc)
        {
            if(showdc)
                new MessageBox("Disconnected from the server", MessageBoxType.OK, "disconnected");
            ReceivedWelcomeMessage = false;
            State = ClientState.Disconnected;
            if (zSocket.Connected)
                zSocket.GetStream().Close();
            zSocket.Close();
            Debug.Write("Client disconnected");
        }

        public void Activity()
        {
            Debug.Print(Globals.Client.zReceiveThread.ThreadState,"Client receiver");
            Debug.Print($"Buffer count: {BufferSize}");
            //early break
            if (State == ClientState.Disconnected)
                return;
            var msg = mDequeueMessage();
            if (msg != null)
                ProcessMessage(msg);
            if (State == ClientState.Connected && zReceiveThread.ThreadState == ThreadState.Stopped)
                Disconnect(true);
            //mobs
            for (int i = Mobs.Count - 1; i >= 0; i--)
                Mobs[i].ClientActivity();
        }

        public void Destroy()
        {
            while (Mobs.Count > 0)
                Mobs.Last.Destroy();
        }            
    }

    public class ClientHero
    {
        public Vector3 Position;
        public HeroClass Class;
        public string Name;
        public int Netid;

        public ClientHero()
        {
            Position = Vector3.Zero;
            Class = HeroClass.Invalid;
            Name = string.Empty;
            Netid = 0;
        }
    }

    public partial class ServerClient
    {
        protected string zIP;
        protected Mutex zMsgMutex;
        protected TcpClient zSocket;
        public int zId;
        protected Queue<Message> zMessages;
        protected Thread zReceiveThread;
        public ClientHero Hero;
        public byte[] zNetidBytes;
        public MapRoom Room;
        protected int BufferSize;

        public string IP { get { return zIP; } }
        public TcpClient Socket { get { return zSocket; } }
        public int Id { get { return zId; } }
        public Thread ReceiveThread { get { return zReceiveThread; } }

        public ServerClient(TcpClient client)
        {
            BufferSize = 0;
            Room = null;
            zMsgMutex = new Mutex();
            zMessages = new Queue<Message>();
            zSocket = client;
            if (client != null)
            {
                zIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                Hero = new ClientHero();
                zReceiveThread = new Thread(new ThreadStart(Receive));
                zReceiveThread.IsBackground = true;
            }
            else
            {
                zId = 0;
                zIP = string.Empty;
                Hero = null;
                zReceiveThread = null;
            }
        }

        /// <summary>
        /// Send message wrapper
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public void SendMessage(byte header,params object[] elements)
        {
            byte[] msg = PackToArray(header,elements);
            zSendMessage(msg);
        }

        /// <summary>
        /// Converts stream of objects to byte array
        /// there should be at least one element which is header
        /// This functions does everything including putting len at 0 index so
        /// only thing that zSendMessage does is write to network stream
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        private byte[] PackToArray(byte header,object[] elements)
        {
            List<byte> list = new List<byte>();
            //len
            list.Add(0);
            //header
            list.Add(header);
            for (int i = 0; i < elements.Length; i++)
            {
                //byte/char
                if (elements[i] is byte || elements[i] is char)
                {
                    list.Add((byte)'c');
                    list.Add(Convert.ToByte(elements[i]));
                }
                //bool
                else if (elements[i] is bool)
                {
                    list.Add((byte)'b');
                    list.Add((bool)elements[i] ? (byte)1 : (byte)0);
                }
                //string
                else if (elements[i] is string)
                {
                    list.Add((byte)'s');
                    list.Add((byte)((string)elements[i]).Length);
                    list.AddRange(Encoding.ASCII.GetBytes((string)elements[i]));
                }
                //int
                else if (elements[i] is int)
                {
                    list.Add((byte)'i');
                    list.AddRange(BitConverter.GetBytes((int)elements[i]));
                }
                //float
                else if (elements[i] is float)
                {
                    list.Add((byte)'f');
                    list.AddRange(BitConverter.GetBytes((float)elements[i]));
                }
                else
                    throw new ArgumentException("Unrecognized type");
            }
            //length
            list[0] = (byte)(list.Count - 1);
            return list.ToArray();
        }

        /// <summary>
        /// Sends message, first byte is length of the message
        /// </summary>
        /// <param name="message"></param>
        public void zSendMessage(byte[] message)
        {
            if (!zSocket.Connected)
                return;
            //upload server
            if (GetType() != typeof(Client))
                Globals.Server.zUpload += message.Length;
            //write to network stream
            NetworkStream ns = zSocket.GetStream();
            ns.Write(message, 0, message.Length);
            ns.Flush();
            //debug
            if (Debug.DebugMode && message[1] != MsgHeader.PlayerPos && message[1] != MsgHeader.Mobpos)
                PrintMsg(message);
        }

        private void PrintMsg(byte[] message)
        {
            string str;
            if (GetType() == typeof(Client))
                str = "to server:";
            else
                str = "to client:";
            for(int i=1;i<message.Length;i++)
                str += $"[{message[i]}]";
            Debug.Write(str);
        }

        /// <summary>
        /// its a small part of the whole msg processing mechanism
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public Message mDequeueMessage()
        {
            //break if there is nothing to read
            Message result = null;
            if (zMessages.Count == 0)
                return null;
            zMsgMutex.WaitOne();
            result = zMessages.Dequeue();
            zMsgMutex.ReleaseMutex();
            return result;
        }

        protected void Receive()
        {
            List<byte> msgBuffer = new List<byte>();
            while (true)
            {
                NetworkStream ns = zSocket.GetStream();
                byte[] bytesReceived = new byte[10025];
                int len = 0;
                try
                {
                    len = ns.Read(bytesReceived, 0, bytesReceived.Length);
                }
                catch (IOException e)
                {
                    //only socket excetion are being handled
                    SocketException se = null;
                    if (e.InnerException.GetType() == typeof(SocketException))
                        se = (SocketException)e.InnerException;
                    else
                        throw e;
                    //acceptable exception is socket exception 10004 when client is disconnecting
                    //another one is 10054 and this is client
                    if (GetType() == typeof(Client))
                    {
                        Client c = (Client)this;
                        if (c.GetState() == ClientState.Disconnected && se.ErrorCode == 10004)
                            return;
                        if (se.ErrorCode == 10054)
                            return;
                        else
                            throw se;
                    }
                    else
                        return;
                }

                //if len is 0 it means that it disconnected
                if(len == 0)
                    break;

                for (int i = 0; i < len; i++)
                    msgBuffer.Add(bytesReceived[i]);                

                CheckAndDispatch(msgBuffer);
                if(Debug.DebugMode)
                    BufferSize = msgBuffer.Count;
            }
        }

        /// <summary>
        /// Checks if complete msg has been received and posts it to Messages if it has
        /// </summary>
        /// <param name="msgBuffer"></param>
        protected void CheckAndDispatch(List<byte> msgBuffer)
        {
            //break if message is not complete
            while (msgBuffer.Count > 0 && msgBuffer.Count > msgBuffer[0])
            {
                Dispatch(msgBuffer);
                msgBuffer.RemoveRange(0, msgBuffer[0] + 1);
            }
        }

        protected void Dispatch(List<byte> msgBuffer)
        {
            var newMsg = new Message();
            newMsg.Data = new byte[msgBuffer[0]];
            msgBuffer.CopyTo(1, newMsg.Data, 0, msgBuffer[0]);
            zMsgMutex.WaitOne();
            zMessages.Enqueue(newMsg);
            zMsgMutex.ReleaseMutex();
        }        
    }
}

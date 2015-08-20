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

    public partial class Server
    {
        private ServerState zState;
        private Mutex zClientMutex;
        private TcpListener zSocket;
        private List<ServerClient> zClients;
        private Thread zAcceptClientsThread;
        private int zNextId;

        public ServerState State { get { return zState; } }
        public int ClientCount { get { return zClients.Count; } }

        public Server()
        {
            zNextId = 1;
            zClientMutex = new Mutex();
            zSocket = null;
            zState = ServerState.Stopped;            
            zClients = new List<ServerClient>();
            zAcceptClientsThread = new Thread(new ThreadStart(AcceptClients));
            zAcceptClientsThread.IsBackground = true;
        }

        public bool Start(int port)
        {            
            try
            {
                zSocket = new TcpListener(IPAddress.Any, port);
                zSocket.Start();
            }
            catch (SocketException se)
            {
                //address/port occupied
                if (se.ErrorCode == 10048)
                    return false;
                throw se;
            }
            zState = ServerState.Running;
            Globals.Write("Server started");
            return true;
        }

        /// <summary>
        /// Starts a thread that accepts new clients
        /// </summary>
        public void StartAccepting()
        {
            Globals.Write("Server now accepting");
            zAcceptClientsThread.Start();
        }

        public int GetNextId()
        {
            int result = zNextId;
            zNextId++;
            return zNextId;
        }
        
        /// <summary>
        /// Stop accepting new clients (but dont stop server)
        /// </summary>
        public void StopAccepting()
        {
            Globals.Write("Server stopped accepting");
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks for disconnected clients
        /// </summary>
        public void Activity()
        {
            Globals.Debug(zClients.Count,"Server client count");
            Globals.Debug(zAcceptClientsThread.ThreadState, "Accepting thread");
            for (int i = zClients.Count - 1; i >= 0; i--)
            {
                Globals.Debug(zClients[i].ReceiveThread.ThreadState, "Client " + i.ToString() + " receiving thread");
                var msg = DequeueMessage(zClients[i]);
                if(msg != null)
                    ProcessMessage(msg, zClients[i]);
                if (zClients[i].ReceiveThread.ThreadState == ThreadState.Stopped)
                    RemoveClient(zClients[i]);
            }
        }

        public void RemoveClient(ServerClient c)
        {
            Globals.Write(c.IP + " disconnected");
            foreach (var c1 in zClients.Where(c2 => c2 != c))
                c1.SendRemovePlayer(c);
            c.Socket.Close();
            zClientMutex.WaitOne();
            zClients.Remove(c);
            zClientMutex.ReleaseMutex();
        }

        /// <summary>
        /// Send message to all clients
        /// </summary>
        /// <param name="message">bytes to send</param>
        public void SendAll(params object[] obj)
        {
            foreach (ServerClient c in zClients)
                c.SendMessage(obj);
        }

        /// <summary>
        /// Returns and removes 
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private Message DequeueMessage(ServerClient c)
        {
            return c.mDequeueMessage();
        }

        public void Stop()
        {
            Globals.Write("Server stopped");
            zState = ServerState.Stopped;
            zSocket.Stop();
            foreach (ServerClient c in zClients)
                c.Socket.Close();
            zClients.Clear();
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
                    var newSocket = zSocket.AcceptTcpClient();
                    var newClient = new ServerClient(newSocket);
                    newClient.zId = GetNextId();
                    newClient.zNetidBytes = BitConverter.GetBytes(newClient.zId);
                    newClient.Hero.Netid = newClient.Id;
                    newClient.ReceiveThread.Start();
                    zClientMutex.WaitOne();
                    zClients.Add(newClient);
                    zClientMutex.ReleaseMutex();
                    //send welcome msg
                    newClient.SendMessage(MsgHeader.Welcome);
                }
            }
            catch (SocketException se)
            {
                if (se.ErrorCode == 10004 && zState == ServerState.Stopped) { }
                else
                    throw se;
            }
        }
    }

    public partial class Client : ServerClient
    {
        private ClientState mState;
        private bool zReceivedWelcomeMessage;
        private float zWelcomeMessageTimeout;

        public Client() : base(null)
        {
            zWelcomeMessageTimeout = 2;
            zReceivedWelcomeMessage = false;
            mState = ClientState.Disconnected;
        }

        public ClientState State  { get {  return mState; } }
        
        /// <summary>
        /// Connect to server, returns true if connected succesfully
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public bool Connect(string host, int port)
        {
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
            Globals.Write("Client connected");
            mState = ClientState.Connected;
            //wait for welcome message
            //its needed because in some cases client will report connected even
            //if it's not
            if (!GetWelcomeMsg())
            {
                Disconnect();
                return false;
            }
            return true;
        }

        private bool GetWelcomeMsg()
        {
            DateTime start = DateTime.Now;
            while (DateTime.Now - start < TimeSpan.FromSeconds(zWelcomeMessageTimeout))
            {
                Activity();
                if (zReceivedWelcomeMessage)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            zReceivedWelcomeMessage = false;
            mState = ClientState.Disconnected;
            if (zSocket.Connected)
                zSocket.GetStream().Close();
            zSocket.Close();
            Globals.Write("Client disconnected");
        }

        public void Activity()
        {
            Globals.Debug(Globals.Client.zReceiveThread.ThreadState,"Client receiver");
            //early break
            if (mState == ClientState.Disconnected)
                return;
            var msg = mDequeueMessage();
            if (msg != null)
                ProcessMessage(msg);
            if (mState == ClientState.Connected && zReceiveThread.ThreadState == ThreadState.Stopped)
                Disconnect();
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

        public string IP { get { return zIP; } }
        public TcpClient Socket { get { return zSocket; } }
        public int Id { get { return zId; } }
        public Thread ReceiveThread { get { return zReceiveThread; } }

        public ServerClient(TcpClient client)
        {                
            zMsgMutex = new Mutex();
            zMessages = new Queue<Message>();
            zSocket = client;
            if (client != null)
            {
                zIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                Hero = new ClientHero();
            }
            else
            {
                zId = 0;
                zIP = string.Empty;
                Hero = null;
            }
            zReceiveThread = new Thread(new ThreadStart(Receive));
            zReceiveThread.IsBackground = true;
        }

        /// <summary>
        /// Send message wrapper
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public void SendMessage(params object[] elements)
        {
            int length = GetSize(elements) + 1;
            byte[] msg = new byte[length];
            //start writing at one because 0th byte carries length
            int index = 1;
            msg[0] = (byte)(length - 1);
            for (int i = 0; i < elements.Length; i++)
            {
                //byte/char
                if (elements[i] is byte || elements[i] is char)
                {
                    msg[index] = Convert.ToByte(elements[i]);
                    index++;
                }
                //bool
                else if (elements[i] is bool)
                {
                    msg[index] = (bool)elements[i] ? (byte)1 : (byte)0;
                    index++;
                }
                //string
                else if (elements[i] is string)
                {
                    byte strLen = (byte)((string)elements[i]).Length;
                    msg[index] = strLen;
                    index++;
                    Array.Copy(Encoding.ASCII.GetBytes((string)elements[i]), 0, msg, index, ((string)elements[i]).Length);
                    index += ((string)elements[i]).Length;
                }
                //int
                else if (elements[i] is int)
                {
                    Array.Copy(BitConverter.GetBytes((int)elements[i]), 0, msg, index, 4);
                    index += 4;
                }
                //float
                else if (elements[i] is float)
                {
                    Array.Copy(BitConverter.GetBytes((float)elements[i]), 0, msg, index, 4);
                    index += 4;
                }
            }
            zSendMessage(msg);
        }

        private static int GetSize(params object[] obj)
        {
            int length = 0;
            for (int i = 0; i < obj.Length; i++)
            {
                if (obj[i] is byte || obj[i] is char || obj[i] is bool)
                    length++;
                else if (obj[i] is string)
                    length += ((string)obj[i]).Length + 1; //extra 1 is to code length before string its one byte so max length is 255
                else if (obj[i] is int)
                    length += 4;
                else if (obj[i] is float)
                    length += 4;
                else
                    throw new ArgumentException("Unrecognizable type");
            }
            return length;
        }

        /// <summary>
        /// Sends message, first byte is length of the message
        /// </summary>
        /// <param name="message"></param>
        public void zSendMessage(byte[] message)
        {
            //write to network stream
            NetworkStream ns = zSocket.GetStream();
            ns.Write(message, 0, message.Length);
            ns.Flush();
        }

        /// <summary>
        /// DONT USE THIS YOU F***R, it's for client and server only
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
                        if (c.State == ClientState.Disconnected && se.ErrorCode == 10004)
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
            }
        }

        /// <summary>
        /// Checks if complete msg has been received and posts it to Messages if it has
        /// </summary>
        /// <param name="msgBuffer"></param>
        protected void CheckAndDispatch(List<byte> msgBuffer)
        {
            //break if message is not complete
            if (msgBuffer.Count <= msgBuffer[0])
                return;
            Dispatch(msgBuffer);
            msgBuffer.RemoveRange(0, msgBuffer[0] + 1);
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

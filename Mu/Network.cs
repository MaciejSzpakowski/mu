﻿using System;
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
    }

    public partial class Server
    {
        private ServerState mState;
        private TcpListener mSocket;
        private List<ServerClient> mClients;
        private Thread mAcceptClientsThread;

        public ServerState State { get { return mState; } }
        public int ClientCount { get { return mClients.Count; } }

        public Server()
        {
            mSocket = null;
            mState = ServerState.Stopped;            
            mClients = new List<ServerClient>();
            mAcceptClientsThread = new Thread(new ThreadStart(AcceptClients));
            mAcceptClientsThread.IsBackground = true;           
        }

        public bool Start(int port)
        {
            mState = ServerState.Running;
            try
            {
                mSocket = new TcpListener(IPAddress.Any, port);
                mSocket.Start();
            }
            catch (SocketException se)
            {
                //address/port occupied
                if (se.ErrorCode == 10048)
                    return false;
                throw se;
            }
            Globals.Write("Server started");
            return true;
        }

        /// <summary>
        /// Starts a thread that accepts new clients
        /// </summary>
        public void StartAccepting()
        {
            Globals.Write("Server now accepting");
            mAcceptClientsThread.Start();
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
            Globals.Debug(mClients.Count,"Server client count");
            Globals.Debug(mAcceptClientsThread.ThreadState, "Accepting thread");
            for (int i = mClients.Count - 1; i >= 0; i--)
            {
                Globals.Debug(mClients[i].ReceiveThread.ThreadState, "Client " + i.ToString() + " receiving thread");
                var msg = DequeueMessage(mClients[i]);
                if(msg != null)
                    ProcessMessage(msg, mClients[i]);
                if (mClients[i].ReceiveThread.ThreadState == ThreadState.Stopped)
                {
                    Globals.Write(mClients[i].IP + " disconnected");
                    mClients[i].Socket.Close();
                    mClients.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Send message to all clients
        /// </summary>
        /// <param name="message">bytes to send</param>
        public void SendAll(byte[] message)
        {
            foreach (ServerClient c in mClients)
                c.SendMessage(message);
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
            mState = ServerState.Stopped;
            mSocket.Stop();
            foreach (ServerClient c in mClients)
                c.Socket.Close();
            mClients.Clear();
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
                    var newSocket = mSocket.AcceptTcpClient();
                    var newClient = new ServerClient(newSocket);
                    Globals.Write("New client:" + newClient.IP);
                    newClient.ReceiveThread.Start();
                    mClients.Add(newClient);
                }
            }
            catch (SocketException se)
            {
                if (se.ErrorCode == 10004 && mState == ServerState.Stopped) { }
                else
                    throw se;
            }
        }
    }

    public partial class Client : ServerClient
    {
        private ClientState mState;
        public Client() : base(null)
        {
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
            mSocket = new TcpClient();
            try
            {
                mSocket.Connect(host, port);
                mReceiveThread.Start();
            }
            catch (SocketException e)
            {
                //10061 usually means that server is not running or not accepting
                if (e.ErrorCode == 10061)
                    return false;
                //no response (timeout)
                if (e.ErrorCode == 10060)
                    return false;
                else
                    throw e;
            }
            Globals.Write("Client connected");
            mState = ClientState.Connected;
            return true;
        }        

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            mState = ClientState.Disconnected;
            if (mSocket.Connected)
                mSocket.GetStream().Close();
            mSocket.Close();
            Globals.Write("Client disconnected");
        }

        public void Activity()
        {
            Globals.Debug(Globals.Client.mReceiveThread.ThreadState,"Client receiver");
            //early break
            if (mState == ClientState.Disconnected)
                return;
            var msg = mDequeueMessage();
            if (msg != null)
                ProcessMessage(msg);
            if (mState == ClientState.Connected && mReceiveThread.ThreadState == ThreadState.Stopped)
                Disconnect();
        }        
    }

    public class ServerClient
    {
        protected string mIP;
        protected Mutex mMsgMutex;
        protected TcpClient mSocket;
        protected int mId;
        protected string mHeroName;
        protected Queue<Message> mMessages;
        protected Thread mReceiveThread;

        public string IP { get { return mIP; } }
        public TcpClient Socket { get { return mSocket; } }
        public int Id { get { return mId; } }
        public string HeroName { get { return mHeroName; } }
        public Thread ReceiveThread { get { return mReceiveThread; } }

        public ServerClient(TcpClient client)
        {
            mMsgMutex = new Mutex();
            mMessages = new Queue<Message>();
            mSocket = client;
            if (client != null)
            {
                mId = client.GetHashCode();
                mIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            }
            else
            {
                mId = 0;
                mIP = string.Empty;
            }
            mHeroName = string.Empty;
            mReceiveThread = new Thread(new ThreadStart(Receive));
            mReceiveThread.IsBackground = true;
        }

        /// <summary>
        /// Sends message, in Mu first byte is length of th message, its added by this function
        /// so you dont have to do it
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(byte[] message)
        {
            //append length
            byte[] bytes = message;
            byte[] msg = new byte[message.Length + 1];
            msg[0] = (byte)message.Length;
            Array.Copy(message, 0, msg, 1, message.Length);
            //write to network stream
            NetworkStream ns = mSocket.GetStream();
            ns.Write(msg, 0, msg.Length);
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
            if (mMessages.Count == 0)
                return null;
            mMsgMutex.WaitOne();
            result = mMessages.Dequeue();
            mMsgMutex.ReleaseMutex();
            return result;
        }

        void Receive()
        {
            List<byte> msgBuffer = new List<byte>();
            while (true)
            {
                NetworkStream ns = mSocket.GetStream();
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
        private void CheckAndDispatch(List<byte> msgBuffer)
        {
            //break if message is not complete
            if (msgBuffer.Count <= msgBuffer[0])
                return;
            Dispatch(msgBuffer);
            msgBuffer.RemoveRange(0, msgBuffer[0] + 1);
        }

        private void Dispatch(List<byte> msgBuffer)
        {
            var newMsg = new Message();
            newMsg.Data = new byte[msgBuffer[0]];
            msgBuffer.CopyTo(1, newMsg.Data, 0, msgBuffer[0]);
            mMsgMutex.WaitOne();
            mMessages.Enqueue(newMsg);
            mMsgMutex.ReleaseMutex();
        }
    }
}

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
                    SendInitData(c);
                    break;
                default:
                    throw new ArgumentException("Unhandled message received by server");
            }
        }

        /// <summary>
        /// Sends newly connected client data about players, monsters and items on the ground
        /// </summary>
        /// <param name="c"></param>
        private void SendInitData(ServerClient c)
        {
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
                default:
                    throw new ArgumentException("Unhandled message received by client");
            }
        }

        public void SendReady()
        {
        }
    }

    public static class MsgHeader
    {
        public const byte Welcome = 1;
        public const byte ClientReady = 2;
    }
}

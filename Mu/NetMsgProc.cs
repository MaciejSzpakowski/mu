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
        }
    }

    public partial class Client : ServerClient
    {
        private void ProcessMessage(Message msg)
        {
            byte[] data = msg.Data;

            //welcome message
            if (IsWelcomeMessage(data))
            {

            }
        }

        private bool IsWelcomeMessage(byte[] data)
        {
            if (data.Length == 3 && data[0] == 1 && data[1] == 2 && data[2] == 3)
                return true;
            else
                return false;
        }
    }
}

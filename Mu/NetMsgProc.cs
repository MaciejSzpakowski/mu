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
        }
    }
}

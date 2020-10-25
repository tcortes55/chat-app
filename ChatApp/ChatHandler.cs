using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApp
{
    public class ChatHandler : WebSocketHandler
    {
        public ChatHandler(ConnectionManager connectionManager) : base(connectionManager)
        {
        }
    }
}

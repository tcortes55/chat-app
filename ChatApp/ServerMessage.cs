using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApp
{
    public class ServerMessage
    {
        public ServerMessage()
        {
            Users = new List<string>();
        }

        public ServerMessage(MessageType messageType, string messageContent, List<string> users)
        {
            Type = messageType.ToString();
            Content = messageContent;
            Users = users;
        }

        public string Type { get; set; }
        public string Content { get; set; }
        public List<string> Users { get; set; }
    }
}

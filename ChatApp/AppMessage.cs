using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApp
{
    public class AppMessage
    {
        public string Type { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Content { get; set; }
        public bool? IsPrivate { get; set; }
    }

    public enum MessageType { CONNECTION, CHAT }
}

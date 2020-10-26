using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApp
{
    public class ClientMessage
    {
        public string Type { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Content { get; set; }
        public bool? IsPrivate { get; set; }

        public bool IsValid(string expectedUsername)
        {
            if (this.IsTypeConnection())
            {
                if (this.Sender == string.Empty)
                {
                    return false;
                }
            }
            else if (this.IsTypeChat())
            {
                if (this.Sender != expectedUsername || this.Content == string.Empty)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        public string BuildChatMessageBody()
        {
            string receiver = this.Receiver == string.Empty ? "Everybody" : this.Receiver;

            return $"{this.Sender} to {receiver}: {this.Content}";
        }

        public string GetMessageType()
        {
            return this.Type.ToUpper();
        }

        public bool IsTypeConnection()
        {
            return this.GetMessageType() == MessageType.CONNECTION.ToString();
        }

        public bool IsTypeChat()
        {
            return this.GetMessageType() == MessageType.CHAT.ToString();
        }
    }

    public enum MessageType { CONNECTION, CHAT }
}

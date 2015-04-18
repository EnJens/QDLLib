using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QDLLib.Preloader
{
    public class MessagePayload : IPreloaderPayload
    {
        public CommandType CommandType
        {
            get { return CommandType.MessageRsp; }
        }

        public string Message
        {
            get;
            private set;
        }

        protected MessagePayload(string message)
        {
            this.Message = message;
        }

        public byte[] Serialize()
        {
            throw new NotImplementedException();
        }

        public static IPreloaderPayload Deserialize(byte[] payload)
        {
            if(payload[0] != (byte)CommandType.MessageRsp)
            {
                throw new ArgumentException("Not a message payload!?");
            }
            string message = ASCIIEncoding.ASCII.GetString(payload, 1, payload.Length - 1);
            return (IPreloaderPayload)new MessagePayload(message);
        }
    }
}

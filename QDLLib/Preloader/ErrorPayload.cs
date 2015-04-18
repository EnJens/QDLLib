using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QDLLib.Preloader
{
    public class ErrorPayload : IPreloaderPayload
    {
        public CommandType CommandType
        {
            get { return CommandType.ErrorRsp; }
        }

        public uint ErrorCode { get; private set; }

        public string Message
        {
            get;
            private set;
        }

        protected ErrorPayload(uint errorcode, string message)
        {
            this.ErrorCode = errorcode;
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
            uint errorcode = (uint)(payload[1] << 24 | payload[2] << 16 | payload[3] << 8 | payload[4]);
            string message = ASCIIEncoding.ASCII.GetString(payload, 5, payload.Length - 5);
            return (IPreloaderPayload)new ErrorPayload(errorcode, message);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QDLLib.Preloader
{
    public class BytePayload : IPreloaderPayload
    {
        public byte[] data { get; private set; }
        public CommandType CommandType { get; private set; }

        public BytePayload(CommandType commandtype, byte[] data)
        {
            this.CommandType = commandtype;
            this.data = data;
            if (this.data == null)
                this.data = new byte[0];
        }

        public byte[] Serialize()
        {
            byte[] ret = new byte[sizeof(byte) + data.Length];
            ret[0] = (byte)this.CommandType;
            Array.Copy(this.data, 0, ret, 1, data.Length);
            return ret;
        }

        public static IPreloaderPayload Deserialize(byte[] payload)
        {
            CommandType commandtype = (CommandType)Enum.ToObject(typeof(CommandType), payload[0]);
            byte[] data = new byte[0];
            if(payload.Length > 1)
            {
                data = new byte[payload.Length - 1];
                Array.Copy(payload, 1, data, 0, data.Length);
            }
            return (IPreloaderPayload)new BytePayload(commandtype, data);
        }
    }
}

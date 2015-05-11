using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QDLLib.Preloader
{
    class WriteFlashPayload : IPreloaderPayload
    {
        public CommandType CommandType { get; private set; }
        public uint Offset { get; private set; }
        public byte[] Data { get; private set; }

        public WriteFlashPayload(uint Offset, byte[] data)
        {
            this.CommandType = Preloader.CommandType.WriteFlashCmd;
            this.Offset = Offset;
            this.Data = data;
        }

        public byte[] Serialize()
        {
            // Offset + data
            byte[] ret = new byte[sizeof(byte) + sizeof(uint) + Data.Length];
            ret[0] = (byte)CommandType;
            ret[1] = (byte)(Offset & 0xFF);
            ret[2] = (byte)(Offset >> 8);
            ret[3] = (byte)(Offset >> 16);
            ret[4] = (byte)(Offset >> 24);
            Array.Copy(Data, 0, ret, 5, Data.Length);
            return ret;
        }

        public static IPreloaderPayload Deserialize(byte[] payload)
        {
            if(payload == null || payload.Length < 5)
            {
                throw new Exception("blah");
            }
            if(payload[0] != (byte)CommandType.WriteFlashCmd && payload[0] != (byte)CommandType.WriteFlashRsp)
            {
                throw new ArgumentException("Not a valid WriteFlashCommand/Response");
            }

            uint offset = (uint)(payload[4] << 24 | payload[3] << 16 | payload[2] << 8 | payload[1]);
            byte[] data = new byte[payload.Length - 5];
            Array.Copy(payload, 5, data, 0, data.Length);

            WriteFlashPayload ret = new WriteFlashPayload(offset, data);
            ret.CommandType = (CommandType)Enum.ToObject(typeof(CommandType), payload[0]);

            return (IPreloaderPayload)ret;
        }

        public override string ToString()
        {
            return String.Format("WriteFlashPayload(Type={0}, Offset={1:X8}, Length={2})", CommandType, Offset, Data.Length);
        }
    }
}

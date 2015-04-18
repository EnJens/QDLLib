using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QDLLib.Preloader;

namespace QDLLib
{
    public class PreloaderCommand
    {

        private const int DELIMITER_LEN = 1;
        private const byte DELIMITER = 0x7E;

        public IPreloaderPayload payload {
            get;
            private set;
        }


        public PreloaderCommand(IPreloaderPayload payload)
        {
            this.payload = payload;
        }

        public byte[] Serialize()
        {
            
            byte[] bytePayload = payload.Serialize();
            ushort crc = Utilities.crc16(bytePayload);
            byte[] crcBytes = escapeBuf(new byte[] { (byte)(crc >> 8), (byte)(crc & 0xFF) });
            byte[] escapedPayload = escapeBuf(bytePayload);
            
            byte[] packet = new byte[DELIMITER_LEN + escapedPayload.Length + crcBytes.Length + DELIMITER_LEN];
            packet[0] = packet[packet.Length - 1] = DELIMITER;
            Array.Copy(escapedPayload, 0, packet, 1, escapedPayload.Length);
            Array.Copy(crcBytes, 0, packet, 1 + escapedPayload.Length, crcBytes.Length);
            
            return packet;
        }

        public static PreloaderCommand Deserialize(byte[] packet)
        {
            return Deserialize(packet, packet.Length);
        }

        public static PreloaderCommand Deserialize(byte[] packet, int length)
        {
            if (packet[0] != DELIMITER || packet[length -  1] != DELIMITER)
            {
                throw new ArgumentException("Not a valid Preloader command");
            }
            byte[] alldata = unescapeBuf(packet, 1, length - 2);
            byte[] payload = new byte[alldata.Length - 2];
            Array.Copy(alldata, payload, payload.Length);

            ushort crc = Utilities.crc16(payload);
            ushort packetCrc = (ushort)(alldata[alldata.Length - 2] << 8 | (alldata[alldata.Length - 1] & 0xFF));

            if(crc != packetCrc)
            {
                Console.WriteLine("Crc not equal!? {0} {1}", crc, packetCrc);
            }

            CommandType commandtype = (CommandType)Enum.ToObject(typeof(CommandType), payload[0]);
            IPreloaderPayload preloaderPayload;
            switch (commandtype)
            {
                case CommandType.MagicRsp:
                    preloaderPayload = MagicPayload.Deserialize(payload);
                    break;
                case CommandType.WriteFlashCmd:
                case CommandType.WriteFlashRsp:
                    preloaderPayload = WriteFlashPayload.Deserialize(payload);
                    break;
                case CommandType.MessageRsp:
                    preloaderPayload = MessagePayload.Deserialize(payload);
                    break;
                case CommandType.ErrorRsp:
                    preloaderPayload = ErrorPayload.Deserialize(payload);
                    break;
                default:
                    preloaderPayload = BytePayload.Deserialize(payload);
                    break;
            }

            return new PreloaderCommand(preloaderPayload);
        }

        private static byte[] unescapeBuf(byte[] packet, int offset, int length)
        {
            int cnt = packet.Skip(offset).Take(length).Count((x) => x == 0x7D);
            byte[] unescaped = new byte[length - cnt];
            int pos = 0;
            for(int i=offset; i<offset + length; i++)
            {
                if (packet[i] == 0x7D)
                {
                    if (packet[i + 1] == 0x5E)
                    {
                        unescaped[pos] = 0x7E;
                    }
                    else if (packet[i + 1] == 0x5D)
                    {
                        unescaped[pos] = 0x7D;
                    }
                    i++;
                }
                else
                    unescaped[pos] = packet[i];
                pos++;
            }
            return unescaped;
        }

        private byte[] escapeBuf(byte[] buffer)
        {
            int cnt = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] == 0x7E || buffer[i] == 0x7D)
                {
                    cnt++;
                }
            }

            byte[] ret = new byte[buffer.Length + cnt];
            int pos = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] == 0x7E)
                {
                    ret[pos] = 0x7D;
                    pos++;
                    ret[pos] = 0x5E;
                    pos++;
                }
                else if (buffer[i] == 0x7D)
                {
                    ret[pos] = 0x7D;
                    pos++;
                    ret[pos] = 0x5D;
                    pos++;
                }
                else
                {
                    ret[pos] = buffer[i];
                    pos++;
                }
            }
            return ret;
        }

    }
}

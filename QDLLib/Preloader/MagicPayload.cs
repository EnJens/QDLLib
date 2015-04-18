using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QDLLib.Preloader
{
    public class MagicPayload : IPreloaderPayload
    {
        private const string MagicResponse = "QCOM fast download protocol targ";

        public CommandType CommandType
        {
            get { return CommandType.MagicRsp; }
        }

        public byte[] Payload { get; private set; }

        protected MagicPayload(byte[] payload)
        {
            this.Payload = payload;
        }

        public byte[] Serialize()
        {
            throw new NotImplementedException();
        }

        public static IPreloaderPayload Deserialize(byte[] payload)
        {
            
            if(payload[0] != (byte)CommandType.MagicRsp ||payload.Length < 36)
            {
                throw new ArgumentException("Not a message payload!?");
            }
            string magicCode = ASCIIEncoding.ASCII.GetString(payload, 1, 32);
            if(!magicCode.Equals(MagicResponse, StringComparison.InvariantCulture))
            {
                throw new ArgumentException("Invalid magic response");
            }

            return (IPreloaderPayload)new MagicPayload(payload);
        }
    }
}

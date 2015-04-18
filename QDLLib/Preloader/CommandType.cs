using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QDLLib.Preloader
{
    // https://github.com/aureljared/unbrick_8960/blob/master/scripts/qdload.pl
    public enum CommandType : byte
    {
        MagicCmd = 0x01,
        MagicRsp = 0x02,
        ExecuteCmd = 0x05,
        ExecuteRsp = 0x06,
        WriteFlashCmd = 0x07,
        WriteFlashRsp = 0x08,
        ResetCmd = 0x0B,
        ResetRsp = 0x0C,
        MessageRsp = 0x0E,
        ErrorRsp = 0x0D,
        CloseFlushCmd = 0x15,
        CloseFlushRsp = 0x16,
        SetSecureModeCmd = 0x17,
        SetSecureModeRsp = 0x18,
        OpenMultiCmd = 0x1B,
        OpenMultiRsp = 0x1C,
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QDLLib.Exceptions
{
    public class QDLResetFailureException : Exception
    {
        public QDLResetFailureException(string message) : base(message)
        {}
    }
}

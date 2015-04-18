using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QDLLib.Exceptions
{
    public class QDLBootstrapFailureException : Exception
    {
        public QDLBootstrapFailureException(string message ) : base(message)
        {}
    }
}

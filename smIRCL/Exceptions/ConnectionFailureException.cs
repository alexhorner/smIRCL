using System;

namespace smIRCL.Exceptions
{
    public class ConnectionFailureException : Exception
    {
        public ConnectionFailureException(string message, Exception innerException = null) : base(message, innerException)
        {
        }
    }
}

using System;

namespace smIRCL.Exceptions
{
    public class ConnectionFailureException : Exception
    {
        /// <summary>
        /// Thrown when an IRC connection fails
        /// </summary>
        /// <param name="message">The reason for the connection failure</param>
        /// <param name="innerException">The exception which caused the connection failure</param>
        public ConnectionFailureException(string message, Exception innerException = null) : base(message, innerException)
        {
        }
    }
}

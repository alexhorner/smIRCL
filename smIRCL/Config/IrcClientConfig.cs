using System;
using smIRCL.Enums;

namespace smIRCL.Config
{
    public class IrcClientConfig
    {
        /// <summary>
        /// The hostname or IP address of the server
        /// </summary>
        public string ServerHostname { get; set; }

        /// <summary>
        /// The port of the server
        /// </summary>
        public int ServerPort { get; set; } = 6667;

        /// <summary>
        /// The connection password of the server
        /// </summary>
        public string ServerPassword { get; set; }

        /// <summary>
        /// The Nick for the client
        /// </summary>
        public string Nick { get; set; }

        /// <summary>
        /// The User name for the client. If unspecified, Nick will be used
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The real name for the client
        /// </summary>
        public string RealName { get; set; }

        /// <summary>
        /// The client's personal authentication password
        /// </summary>
        public string AuthPassword { get; set; }

        /// <summary>
        /// The client's personal authentication method
        /// </summary>
        public AuthMode AuthMode { get; set; } = AuthMode.None;

        /// <summary>
        /// Whether to use SSL for the connection
        /// </summary>
        public bool UseSsl { get; set; }

        /// <summary>
        /// Checks this configuration is valid
        /// </summary>
        /// <param name="throwOnValidationError">Whether to throw an exception on a validation error rather than return false</param>
        /// <returns><c>bool</c> Is the configuration valid</returns>
        public bool IsValid(bool throwOnValidationError = false)
        {
            bool isValid = true;

            if (string.IsNullOrWhiteSpace(ServerHostname))
            {
                if (throwOnValidationError) throw new Exception($"The parameter '{nameof(ServerHostname)}' is invalid");
                isValid = false;
            }

            if (ServerPort > 65535 || ServerPort < 1)
            {
                if (throwOnValidationError) throw new Exception($"The parameter '{nameof(ServerPort)}' is out of range 1-65535");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(Nick))
            {
                if (throwOnValidationError) throw new Exception($"The parameter '{nameof(Nick)}' is invalid");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(UserName))
            {
                if (throwOnValidationError) throw new Exception($"The parameter '{nameof(UserName)}' is invalid");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(RealName))
            {
                if (throwOnValidationError) throw new Exception($"The parameter '{nameof(RealName)}' is invalid");
                isValid = false;
            }

            if (AuthMode != AuthMode.None && string.IsNullOrWhiteSpace(AuthPassword))
            {
                if (throwOnValidationError) throw new Exception($"The parameter '{nameof(AuthPassword)}' is invalid for AuthMode {AuthMode.ToString()}");
                isValid = false;
            }

            return isValid;
        }
    }
}

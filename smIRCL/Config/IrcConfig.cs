using System;
using System.Collections.Generic;
using smIRCL.Enums;

namespace smIRCL.Config
{
    public class IrcConfig
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
        /// A list of alternative Nicks if the preferred one is unavailable
        /// </summary>
        public Queue<string> AlternativeNicks { get; set; } = new Queue<string>();

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
        /// How many times to attempt a reconnect on failure
        /// </summary>
        public int ReconnectAttempts { get; set; } = 3;

        /// <summary>
        /// Whether to connect immediately during IrcClient instantiation
        /// </summary>
        public bool ConnectOnInstantiation { get; set; } = false;

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

            foreach (string nick in AlternativeNicks)
            {
                if (string.IsNullOrWhiteSpace(nick))
                {
                    if (throwOnValidationError) throw new Exception($"One of the values in the parameter '{nameof(AlternativeNicks)}' is invalid");
                    isValid = false;
                }
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

            if (ReconnectAttempts < 0)
            {
                if (throwOnValidationError) throw new Exception($"The parameter '{nameof(ReconnectAttempts)}' is less than 0");
                isValid = false;
            }

            return isValid;
        }
    }
}

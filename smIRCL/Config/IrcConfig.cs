using System;
using System.Collections.Generic;
using smIRCL.Enums;
using smIRCL.Extensions;
using smIRCL.ServerEntities;

namespace smIRCL.Config
{
    /// <summary>
    /// Generalised IRC configuration with defaults
    /// </summary>
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
        /// A list of channels to automatically join upon connection completion
        /// </summary>
        public List<string> AutoJoinChannels { get; set; } = new List<string>();

        /// <summary>
        /// The User name for the client
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The real name for the client
        /// </summary>
        public string RealName { get; set; }

        /// <summary>
        /// The client's personal authentication username
        /// </summary>
        public string AuthUsername { get; set; }

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
        /// The time an IrcUser will be safe from garbage collection after a Direct Message, with no mutual channels
        /// </summary>
        public TimeSpan DirectMessageHoldingPeriod { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The IRCv3 capabilities to negotiate. Default capabilities are included and should be added to, not replaced otherwise some features may be broken. SASL must not be included as it is determined by AuthMode
        /// </summary>
        public List<string> DesiredCapabilities = new List<string>
        {
            "message-tags",
            "away-notify",
            "extended-join",
            "multi-prefix",
            "chghost"
        };

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

            foreach (string channel in AutoJoinChannels)
            {
                if (string.IsNullOrWhiteSpace(channel) || !IrcChannel.IsValidName(channel))
                {
                    if (throwOnValidationError) throw new Exception($"One of the values in the parameter '{nameof(AutoJoinChannels)}' is invalid");
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

            if (AuthMode != AuthMode.None && string.IsNullOrWhiteSpace(AuthUsername))
            {
                if (throwOnValidationError) throw new Exception($"The parameter '{nameof(AuthUsername)}' is invalid for AuthMode {AuthMode.ToString()}");
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

            foreach (string capability in DesiredCapabilities)
            {
                if (string.IsNullOrWhiteSpace(capability))
                {
                    if (throwOnValidationError) throw new Exception($"One of the values in the parameter '{nameof(DesiredCapabilities)}' is invalid");
                    isValid = false;
                }

                if (capability.ToIrcLower() == "sasl")
                {
                    if (throwOnValidationError) throw new Exception($"Do not provide SASL as a value in the parameter '{nameof(DesiredCapabilities)}'");
                    isValid = false;
                }
            }

            return isValid;
        }
    }
}

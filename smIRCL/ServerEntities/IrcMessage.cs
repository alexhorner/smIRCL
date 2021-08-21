using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using smIRCL.Constants;

namespace smIRCL.ServerEntities
{
    /// <summary>
    /// An IRC message received from an IRC server in its parsed parts
    /// </summary>
    public class IrcMessage
    {
        /// <summary>
        /// IRCv3 message tags and their values
        /// </summary>
        public ReadOnlyCollection<KeyValuePair<string, string>> Tags => new(TagsInternal);
        /// <summary>
        /// IRCv3 message tags and their values (internal)
        /// </summary>
        protected internal readonly List<KeyValuePair<string, string>> TagsInternal;

        /// <summary>
        /// The hostmask of the message sender
        /// </summary>
        public readonly string SourceHostMask;

        /// <summary>
        /// The Nick of the message sender
        /// </summary>
        public readonly string SourceNick;

        /// <summary>
        /// The User name of the message sender
        /// </summary>
        public readonly string SourceUserName;

        /// <summary>
        /// The hostname of the message sender
        /// </summary>
        public readonly string SourceHost;

        /// <summary>
        /// The IRC command issued
        /// </summary>
        public readonly string Command;

        /// <summary>
        /// Parameters to the IRC command issued
        /// </summary>
        public ReadOnlyCollection<string> Parameters => new(ParametersInternal);
        /// <summary>
        /// Parameters to the IRC command issued (internal)
        /// </summary>
        protected internal readonly List<string> ParametersInternal;

        /// <summary>
        /// Instantiates a new IRC command message
        /// </summary>
        /// <param name="parameters">The parameters to the command</param>
        /// <param name="tags">The tags assigned to the command</param>
        /// <param name="sourceHostMask">The hostmask of the source sender</param>
        /// <param name="sourceNick">The nick of the source sender</param>
        /// <param name="sourceUserName">The username of the source sender</param>
        /// <param name="sourceHost">The hostname of the source sender</param>
        /// <param name="command">The command of this command message</param>
        public IrcMessage(List<string> parameters, List<KeyValuePair<string, string>> tags, string sourceHostMask, string sourceNick, string sourceUserName, string sourceHost, string command)
        {
            ParametersInternal = parameters;
            TagsInternal = tags;
            SourceHostMask = sourceHostMask;
            SourceNick = sourceNick;
            SourceUserName = sourceUserName;
            SourceHost = sourceHost;
            Command = command;
        }

        /// <summary>
        /// Parses a raw IRC message from a string to its appropriate parts
        /// </summary>
        /// <param name="message">The raw IRC message to parse</param>
        /// <returns>The parsed object representing all parts of the raw message</returns>
        public static IrcMessage Parse(string message)
        {
            if (String.IsNullOrWhiteSpace(message)) return null;

            #region Split Tags from Message

            string tagsEncoded;
            string standardMessage;

            if (message.StartsWith("@"))
            {
                message = message.TrimStart('@');
                string[] tagAndMessage = message.Split(new[] { ' ' }, 2);
                tagsEncoded = tagAndMessage[0].TrimEnd(';');
                standardMessage = tagAndMessage[1];
            }
            else
            {
                tagsEncoded = null;
                standardMessage = message;
            }

            #endregion

            #region Decode Tags

            List<KeyValuePair<string, string>> tags = new List<KeyValuePair<string, string>>(); //New list for tags after parsing

            if (tagsEncoded != null)
            {
                string[] tagPairs = tagsEncoded.Split(';'); //Split the tag list into pairs of tags

                foreach (string tagPair in tagPairs)
                {
                    List<string> keyValue = tagPair.Split('=').ToList();

                    string key = keyValue[0]; //Extract the key
                    keyValue.RemoveAt(0); //Remove the key from the split list

                    string value = keyValue.Count > 0 ? "" : null; //Check if there is a value
                    string unescapedValue = "";

                    if (keyValue.Count > 0) //If there are value parts
                    {
                        foreach (string part in keyValue) //Concatenate the parts again with the = removed by the split put back because we clearly split too many times
                        {
                            if (value == "")
                            {
                                value += part;
                            }
                            else
                            {
                                value += "=" + part;
                            }
                        }
                    }

                    if (value != null)
                    {
                        unescapedValue = UnescapeTagValue(value);
                    }


                    tags.RemoveAll(t => t.Key == key); //Remove any tags with the same key as the spec says keep only the final value
                    tags.Add(new KeyValuePair<string, string>(key, unescapedValue));
                }
            }

            #endregion

            #region Split Parts and Parameter

            string splitParameterBy = " :";
            int parameterIndex = standardMessage.IndexOf(splitParameterBy, StringComparison.Ordinal);

            string messagePartsUnsplit;
            string parameter = null;

            if (parameterIndex > -1)
            {
                messagePartsUnsplit = standardMessage.Substring(0, parameterIndex);
                parameter = standardMessage.Substring(parameterIndex + splitParameterBy.Length);
            }
            else
            {
                messagePartsUnsplit = standardMessage;
            }

            List<string> messageParts = messagePartsUnsplit.Split(' ').ToList(); //Split message parts into list of parts

            #endregion

            #region Extract Source Details
            string sourceHostMask = messageParts[0].StartsWith(":") ? messageParts[0].TrimStart(':') : null; //If first part is a hostmask, grab it
            string sourceNick = null;
            string sourceUserName = null;
            string sourceHost = null;

            if (sourceHostMask != null)
            {
                messageParts.RemoveAt(0); //If a hostmask was grabbed, remove it from the parts list

                string[] detailsAndHost = sourceHostMask.Split('@');
                if (detailsAndHost.Length > 1) sourceHost = detailsAndHost[1];

                string[] nickAndUserName = detailsAndHost[0].Split('!');
                if (nickAndUserName.Length > 1) sourceUserName = nickAndUserName[1];

                sourceNick = nickAndUserName[0];
            }

            #endregion

            #region Grab Final Command and Parameters

            string command = messageParts[0]; //First (remaining) item in list should be the command, grab it
            messageParts.RemoveAt(0); //Remove command from list after grabbing

            if (parameter != null) messageParts.Add(parameter); //Add the last parameter to the list which is now just a list of parameters

            #endregion

            return new IrcMessage(messageParts, tags, sourceHostMask, sourceNick, sourceUserName, sourceHost, command);
        }

        private static string UnescapeTagValue(string tagValue)
        {
            string unescapedValue = "";
            Queue<char> escaped = new Queue<char>();

            foreach (char character in tagValue)
            {
                escaped.Enqueue(character);
            }

            while (escaped.Count > 0)
            {
                char current = escaped.Dequeue();

                if (current == '\\')
                {
                    if (escaped.Count > 0)
                    {
                        char next = escaped.Dequeue();
                        string full = current.ToString() + next.ToString();

                        unescapedValue += GeneralConstants.TagEscapeDictionary.Any(te => te.Key == full) ? GeneralConstants.TagEscapeDictionary[full] : next.ToString();
                    }
                }
                else
                {
                    unescapedValue += current.ToString();
                }
            }

            return unescapedValue;
        }
    }
}

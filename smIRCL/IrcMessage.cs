using System;
using System.Collections.Generic;
using System.Linq;

namespace smIRCL
{
    /// <summary>
    /// Represents a parsed IRC message
    /// </summary>
    public class IrcMessage
    {
        /// <summary>
        /// If available, represents a list of tags included in the message, otherwise null
        /// </summary>
        public List<KeyValuePair<string, string>> Tags { get; set; }

        /// <summary>
        /// If available, represents the hostmask of a message, otherwise null
        /// </summary>
        public string HostMask { get; set; }

        /// <summary>
        /// The command in the message
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// The parameters of the command
        /// </summary>
        public List<string> Parameters { get; set; }




        /// <summary>
        /// Parses an IRC message from a string to an IrcMessage object
        /// </summary>
        /// <param name="message">The message to parse</param>
        /// <returns></returns>
        public static IrcMessage ParseMessage(string message)
        {
            if (String.IsNullOrWhiteSpace(message)) throw new ArgumentException($"The parameter '{nameof(message)}' is null or whitespace");

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

            string[] messageAndFinalParameter = standardMessage.Split(new[] { " :" }, StringSplitOptions.None); //Split message parts and final parameter

            string messagePartsUnsplit = messageAndFinalParameter[0]; //Message parts alone
            string parameter = messageAndFinalParameter[1]; //Final parameter alone

            List<string> messageParts = messagePartsUnsplit.Split(' ').ToList(); //Split message parts into list of parts

            #endregion

            #region Extract HostMask
            string hostMask = messageParts[0].StartsWith(":") ? messageParts[0].TrimStart(':') : null; //If first part is a hostmask, grab it
            if (hostMask != null) messageParts.RemoveAt(0); //If a hostmask was grabbed, remove it from the parts list

            #endregion

            #region Grab Final Command and Parameters

            string command = messageParts[0]; //First (remaining) item in list should be the command, grab it
            messageParts.RemoveAt(0); //Remove command from list after grabbing

            messageParts.Add(parameter); //Add the last parameter to the list which is now just a list of parameters

            #endregion

            return new IrcMessage
            {
                Tags = tags,
                HostMask = hostMask,
                Command = command,
                Parameters = messageParts
            };
        }

        /// <summary>
        /// Takes a tag's escaped value and unescapes it
        /// </summary>
        /// <param name="tagValue">The escaped value</param>
        /// <returns></returns>
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

                        unescapedValue += Constants.TagEscapeDictionary.Any(te => te.Key == full) ? Constants.TagEscapeDictionary[full] : next.ToString();
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

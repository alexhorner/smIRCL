using System;
using System.Collections.Generic;
using System.Linq;

namespace smIRCL
{
    public class IrcMessage
    {
        public List<KeyValuePair<string, string>> Tags { get; set; }


        public string SourceHostMask { get; set; }
        public string SourceNick { get; set; }
        public string SourceUserName { get; set; }
        public string SourceHost { get; set; }


        public string Command { get; set; }

        public List<string> Parameters { get; set; }


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
            int parameterIndex = standardMessage.IndexOf(splitParameterBy);

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

            messageParts.Add(parameter); //Add the last parameter to the list which is now just a list of parameters

            #endregion

            return new IrcMessage
            {
                Tags = tags,
                SourceHostMask = sourceHostMask,
                SourceNick = sourceNick,
                SourceUserName = sourceUserName,
                SourceHost = sourceHost,
                Command = command.ToUpper(),
                Parameters = messageParts
            };
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

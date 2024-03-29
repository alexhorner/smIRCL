﻿using System;
using smIRCL.EventArgs;
using smIRCL.ServerEntities;

// ReSharper disable EventNeverSubscribedTo.Global

namespace smIRCL.Core
{
    public partial class IrcController
    {
        /// <summary>
        /// A handler for private (direct) messages
        /// </summary>
        /// <param name="controller">The source controller</param>
        /// <param name="args">The message data</param>
        public delegate void PrivateMessageHandler(IrcController controller, PrivateMessageEventArgs args);
        
        /// <summary>
        /// Fired when a private (direct) message is received
        /// </summary>
        public event PrivateMessageHandler OnPrivateMessage;

        
        /// <summary>
        /// A handler for channel messages
        /// </summary>
        /// <param name="controller">The source controller</param>
        /// <param name="args">The message data</param>
        public delegate void ChannelMessageHandler(IrcController controller, ChannelMessageEventArgs args);
        
        /// <summary>
        /// Fired when a channel message is received
        /// </summary>
        public event ChannelMessageHandler OnChannelMessage;
        
        
        /// <summary>
        /// A handler for private (direct) CTCP messages
        /// </summary>
        /// <param name="controller">The source controller</param>
        /// <param name="args">The message data</param>
        public delegate void PrivateCtcpMessageHandler(IrcController controller, PrivateCtcpMessageEventArgs args);
        
        /// <summary>
        /// Fired when a private (direct) CTCP message is received
        /// </summary>
        public event PrivateCtcpMessageHandler OnPrivateCtcpMessage;

        
        /// <summary>
        /// A handler for channel CTCP messages
        /// </summary>
        /// <param name="controller">The source controller</param>
        /// <param name="args">The message data</param>
        public delegate void ChannelCtcpMessageHandler(IrcController controller, ChannelCtcpMessageEventArgs args);
        
        /// <summary>
        /// Fired when a channel CTCP message is received
        /// </summary>
        public event ChannelCtcpMessageHandler OnChannelCtcpMessage;
        
        
        /// <summary>
        /// A handler for private (direct) notices
        /// </summary>
        /// <param name="controller">The source controller</param>
        /// <param name="args">The message data</param>
        public delegate void PrivateNoticeHandler(IrcController controller, PrivateNoticeEventArgs args);
        
        /// <summary>
        /// Fired when a private (direct) notice is received
        /// </summary>
        public event PrivateNoticeHandler OnPrivateNotice;
        
        
        /// <summary>
        /// A handler for channel notices
        /// </summary>
        /// <param name="controller">The source controller</param>
        /// <param name="args">The notice data</param>
        public delegate void ChannelNoticeHandler(IrcController controller, ChannelNoticeEventArgs args);
        
        /// <summary>
        /// Fired when a channel notice is received
        /// </summary>
        public event ChannelNoticeHandler OnChannelNotice;
        
        
        /// <summary>
        /// A handler for client error handling
        /// </summary>
        /// <param name="controller">The source controller</param>
        /// <param name="message">The command message which resulted in the error</param>
        /// <param name="exception">The error that occured</param>
        public delegate void ClientErroredHandler(IrcController controller, IrcMessage message, Exception exception);
        
        /// <summary>
        /// Fired when a client error occurs on the controller's event hooks. Examples: <see cref="OnChannelMessage"/>, <see cref="OnPrivateNotice"/>
        /// </summary>
        public event ClientErroredHandler OnClientError;
        
        /// <summary>
        /// Fired when a client error occurs on the controller's event name based <see cref="Handlers"/>
        /// </summary>
        public event ClientErroredHandler OnClientBaseError;
    }
}
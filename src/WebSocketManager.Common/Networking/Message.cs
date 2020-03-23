﻿namespace WebSocketManager.Common
{
    public enum MessageType
    {
        Raw,
        Text,
        MethodInvocation,
        ConnectionEvent,
        MethodReturnValue
    }

    public class Message
    {
        public MessageType MessageType { get; set; }
        public string Data { get; set; }
    }
}
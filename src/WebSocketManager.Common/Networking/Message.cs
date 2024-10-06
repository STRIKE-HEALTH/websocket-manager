namespace WebSocketManager.Common
{
    public enum MessageType
    {

        Text,
        MethodInvocation,
        ConnectionEvent,
        MethodReturnValue,
        Ping,
        Pong
       
    }

    public class Message
    {
        public MessageType MessageType { get; set; }
        public string Channel { get; set; }
        public string Data { get; set; }
    }
}
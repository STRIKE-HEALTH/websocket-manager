using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketManager.Common
{
    
    public class RawStringInvocationStrategy:MethodInvocationStrategy
    {
        /// <summary>
        /// Called when an invoke method call has been received.
        /// </summary>
        /// <param name="socket">The web-socket of the client that wants to invoke a method.</param>
        /// <param name="invocationDescriptor">
        /// The invocation descriptor containing the method name and parameters.
        /// </param>
        /// <returns>Awaitable Task.</returns>

        public override async Task<object> OnInvokeMethodReceivedAsync(WebSocket socket, InvocationDescriptor invocationDescriptor)
        {
            return await ReceiveStringAsync(socket, invocationDescriptor);
        }
        public async Task<object> ReceiveStringAsync(WebSocket socket, InvocationDescriptor invocationDescriptor)
        {
            if (_recieveMessageHandler != null)
            {
                Func<object[], object> Handler = (args) => { _recieveMessageHandler(args); return null; };
                return await Task.Run(() => Handler(invocationDescriptor.Arguments));
            }
            else
                return await Task.FromResult<object>(null);

        }

        Action<object[]> _recieveMessageHandler;
        /// <summary>
        /// Registers the specified method name and calls the action.
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="handler">The handler action with arguments.</param>
        public void OnRecieveMessage(Action<object[]> handler)
        {
            _recieveMessageHandler = handler;
        }
    }
}

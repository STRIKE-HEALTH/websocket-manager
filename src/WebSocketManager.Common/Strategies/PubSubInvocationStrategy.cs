using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebSocketManager.Common
{
    /// <summary>
    /// The string method invocation strategy. Finds methods by registering the names and callbacks.
    /// </summary>
    public class PubSubStrategy : MethodInvocationStrategy
    {
        /// <summary>
        /// The registered handlers.
        /// </summary>
        private Dictionary<string, List<InvocationHandler>> _handlers = new Dictionary<string, List<InvocationHandler>>();

        /// <summary>
        /// Registers the specified method name and calls the action.
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="handler">The handler action with arguments.</param>
        public void Subscribe(string channel, Action<object[]> handler)
        {
            var invocationHandler = new InvocationHandler(handler, new Type[] { });
            if (!_handlers.ContainsKey(channel))
                _handlers.Add(channel,new List<InvocationHandler>());
            _handlers[channel].Add(invocationHandler);
        }

        /// <summary>
        /// Registers the specified method name and calls the function.
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="handler">The handler function with arguments and return value.</param>
        public void Subscribe(string channel, Func<object[], object> handler)
        {
            var invocationHandler = new InvocationHandler(handler, new Type[] { });

            if (!_handlers.ContainsKey(channel))
                _handlers.Add(channel, new List<InvocationHandler>());
            _handlers[channel].Add(invocationHandler);
            //_handlers.Add(methodName, invocationHandler);
        }

        private class InvocationHandler
        {
            public Func<object[], object> Handler { get; set; }
            public Type[] ParameterTypes { get; set; }

            public InvocationHandler(Func<object[], object> handler, Type[] parameterTypes)
            {
                Handler = handler;
                ParameterTypes = parameterTypes;
            }

            public InvocationHandler(Action<object[]> handler, Type[] parameterTypes)
            {
                Handler = (args) => { handler(args); return null; };
                ParameterTypes = parameterTypes;
            }
        }
        Action<object[]> _recieveMessageHandler;
        public void OnRecieveMessage(Action<object[]> handler)
        {
            _recieveMessageHandler = handler;
        }

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
            
            
                if (_recieveMessageHandler != null)
                {
                    Func<object[], object> Handler = (args) => { _recieveMessageHandler(args); return null; };
                    _ = Task.Run(() => Handler(invocationDescriptor.Arguments));
                }
                
            
                var channel = invocationDescriptor.Channel;
                //var publishHandlersForChannel = _handlers[channel];

                var publishHandlersForChannel = from result in _handlers
                              where Regex.Match(channel, result.Key, RegexOptions.Singleline|RegexOptions.IgnoreCase).Success
                              select result.Value;

                await Task.Run(() => {

                    foreach (var handlers in publishHandlersForChannel)
                    foreach (var handle in handlers)
                        handle.Handler(invocationDescriptor.Arguments);

                    return new object();
                });
            

            if (!_handlers.ContainsKey(invocationDescriptor.MethodName))
                throw new Exception($"Received unknown command '{invocationDescriptor.MethodName}'.");
            var invocationHandler = _handlers[invocationDescriptor.MethodName];
            if (invocationHandler != null)
            {
                return await Task.Run(() => {

                    foreach (var handle in invocationHandler)
                         invocationHandler[0].Handler(invocationDescriptor.Arguments);

                    return new object();
                });
            }
            return await Task.FromResult<object>(null);
        }
    }
}
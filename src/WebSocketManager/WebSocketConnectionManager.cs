using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketManager
{
    public class InternalSocket : WebSocket
    {
        private WebSocket _websocket;
       
        private string Id { get; set; }
        public bool Errored { get; set; }
        public WebSocket GetSocket()
        {
            return _websocket;
        }
        public InternalSocket(WebSocket socket, string id)
        {
            _websocket = socket;
            Id = id;
            Errored = false;
        }
        public override WebSocketCloseStatus? CloseStatus => _websocket.CloseStatus;

        public override string CloseStatusDescription => _websocket.CloseStatusDescription;

        public override WebSocketState State => _websocket.State;

        public override string SubProtocol => _websocket.SubProtocol;

        public override void Abort()
        {
            _websocket.Abort();
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            return _websocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            return _websocket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
        }

        public override void Dispose()
        {
            _websocket.Dispose();
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            return _websocket.ReceiveAsync(buffer, cancellationToken);
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            return _websocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
        }
    }

    public class WebSocketConnectionManager
    {
        private ConcurrentDictionary<string, List<InternalSocket>> _sockets = new ConcurrentDictionary<string, List<InternalSocket>>();
        private ConcurrentDictionary<string, List<string>> _groups = new ConcurrentDictionary<string, List<string>>();
        private ConcurrentDictionary<string, bool> _active = new ConcurrentDictionary<string, bool>();
        Random rnd = new Random();
        
        public WebSocket GetSocketById(string id)
        {
            if (id == null)
                return null;
            _sockets.TryGetValue(id, out var socket);

            if(socket != null && socket.Count > 0)
            {
                int socketChoice = 0;
                if (socket.Count > 1)// if we have more than one socket for this id, we will return a random one
                    socketChoice = rnd.Next(0, socket.Count);
                return socket[socketChoice];
            }
                
            return null; 
        }

        public ConcurrentDictionary<string, List<InternalSocket>> GetAll()
        {
            return _sockets;
        }

        public List<string> GetAllFromGroup(string GroupID)
        {
            if (_groups.ContainsKey(GroupID))
            {
                return _groups[GroupID];
            }

            return default(List<string>);
        }

        public string GetId(WebSocket socket)
        {
            foreach(var kv in _sockets)
            {
                foreach(var s in kv.Value)
                    if (socket == s.GetSocket())
                        return kv.Key;
            }
            return null;

            //return _sockets.FirstOrDefault(p => p.Value == socket).Key;
        }

        public string AddSocket(WebSocket socket)
        {
            var id = CreateConnectionId();
            lock (_sockets)
            {
                _sockets.TryAdd(id, new List<InternalSocket> { new InternalSocket(socket,id) });
                _active.TryAdd(id, true);
            }
            return id;
        }

        public void AddSocketWithId(string id,WebSocket socket)
        {
            lock (_sockets)
            {
                if (_sockets.ContainsKey(id))
                    _sockets[id].Add(new InternalSocket(socket,id));
                else
                    _sockets.TryAdd(id, new List<InternalSocket> { new InternalSocket(socket,id) });
            }
           
        }

        public void AddToGroup(string socketID, string groupID)
        {
            lock (_groups)
            {
                if (_groups.ContainsKey(groupID))
                {
                    if (!_groups[groupID].Contains(socketID))
                        _groups[groupID].Add(socketID);

                    return;
                }

                _groups.TryAdd(groupID, new List<string> { socketID });
            }
        }

        public void RemoveFromGroup(string socketID, string groupID)
        {
            lock (_groups)
            {
                if (_groups.ContainsKey(groupID))
                {
                    _groups[groupID].Remove(socketID);
                }
            }
        }

        public async Task RemoveSocket(string id)
        {
            if (id == null) return;
            bool active;
            _sockets.TryGetValue(id, out var sockets);
            foreach (var group in _groups.Keys)
            {
                RemoveFromGroup(id,group);

            }
            foreach (var socket in sockets)
            {
                if (socket.State != WebSocketState.Open) return;

                await socket.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure,
                                        statusDescription: "Closed by the WebSocketManager",
                                        cancellationToken: CancellationToken.None).ConfigureAwait(false);
            }
            lock (_sockets)
            {
                _sockets.TryRemove(id, out sockets);
                _active.TryRemove(id, out active);
            }
        }
        public async Task RemoveSocket(WebSocket socket)
        {
            var id = GetId(socket);
            if (id == null) return;
            bool active;
            lock (_sockets)
            {
                _sockets.TryGetValue(id, out var sockets);
                sockets.RemoveAll(x=>x.GetSocket()==socket);
                if (sockets.Count == 0)
                {
                    _active.TryRemove(id, out active);
                    foreach (var group in _groups.Keys)
                    {
                        RemoveFromGroup(id, group);

                    }
                }
            }
            
            if (socket.State != WebSocketState.Open) return;

            await socket.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure,
                                    statusDescription: "Closed by the WebSocketManager",
                                    cancellationToken: CancellationToken.None).ConfigureAwait(false);
            


        }

        public bool IsSocketActive(string id)
        {
            bool returnValue;
            _active.TryGetValue(id,out returnValue);
            return returnValue;
        }
        public void MarkSocketActive(string socketId)
        {
            _active.AddOrUpdate(socketId, true, (key, oldValue) => true);
        }
        public void MarkSocketInactive(string socketId)
        {
            _active.AddOrUpdate(socketId, false, (key, oldValue) => false);
        }
       
        private string CreateConnectionId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}

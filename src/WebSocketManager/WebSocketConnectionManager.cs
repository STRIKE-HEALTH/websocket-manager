﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketManager
{
    public class WebSocketConnectionManager
    {
        private ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();
        private ConcurrentDictionary<string, List<string>> _groups = new ConcurrentDictionary<string, List<string>>();
        private ConcurrentDictionary<string, bool> _active = new ConcurrentDictionary<string, bool>();
        public WebSocket GetSocketById(string id)
        {
            if (id == null)
                return null;
            _sockets.TryGetValue(id, out var socket);
            return socket;
        }

        public ConcurrentDictionary<string, WebSocket> GetAll()
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
            return _sockets.FirstOrDefault(p => p.Value == socket).Key;
        }

        public string AddSocket(WebSocket socket)
        {
            var id = CreateConnectionId();
            _sockets.TryAdd(id, socket);
            _active.TryAdd(id, true);
            return id;
        }

        public void AddSocketWithId(string id,WebSocket socket)
        {
            if(_sockets.ContainsKey(id))
                _sockets[id] =socket;
            else
                _sockets.TryAdd(id, socket);
           
        }

        public void AddToGroup(string socketID, string groupID)
        {
            if (_groups.ContainsKey(groupID))
            {
                if(!_groups[groupID].Contains(socketID))
                    _groups[groupID].Add(socketID);

                return;
            }

            _groups.TryAdd(groupID, new List<string> { socketID });
        }

        public void RemoveFromGroup(string socketID, string groupID)
        {
            if (_groups.ContainsKey(groupID))
            {
                _groups[groupID].Remove(socketID);
            }
        }

        public async Task RemoveSocket(string id)
        {
            if (id == null) return;

               
            WebSocket socket;
            bool active;
            _sockets.TryRemove(id, out socket);
            _active.TryRemove(id, out active);
            foreach (var group in _groups.Keys)
            {
                RemoveFromGroup(id,group);

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

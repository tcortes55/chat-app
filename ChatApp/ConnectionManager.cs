using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace ChatApp
{
    public class ConnectionManager
    {
        private static ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();
        private static ConcurrentDictionary<string, string> _users = new ConcurrentDictionary<string, string>();

        public WebSocket GetSocketById(string id)
        {
            return _sockets.FirstOrDefault(p => p.Key == id).Value;
        }

        public ConcurrentDictionary<string, WebSocket> GetAllSockets()
        {
            return _sockets;
        }

        public List<string> GetAllUsernames()
        {
            return _users.Select(p => p.Key).ToList();
        }

        public string GetId(WebSocket socket)
        {
            return _sockets.FirstOrDefault(p => p.Value == socket).Key;
        }

        public string GetUsernameBySocketId(string socketId)
        {
            return _users.FirstOrDefault(p => p.Value == socketId).Key;
        }

        public string GetUsernameBySocket(WebSocket socket)
        {
            string socketId = GetId(socket);
            return GetUsernameBySocketId(socketId);
        }

        public void AddSocket(WebSocket socket)
        {
            string socketId = CreateConnectionId();
            _sockets.TryAdd(socketId, socket);
        }

        public void AddUser(WebSocket socket, string username)
        {
            string socketId = GetId(socket);
            _users.TryAdd(username, socketId);
        }

        public async Task RemoveSocket(WebSocket socket, string description = "Connection closed")
        {
            string id = GetId(socket);
            if (!string.IsNullOrEmpty(id))
            {
                _sockets.TryRemove(id, out _);
            }

            if (socket.State != WebSocketState.Aborted)
            {
                await socket.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure,
                                    statusDescription: description,
                                    cancellationToken: CancellationToken.None);
            }
        }

        public void RemoveUser(string username)
        {
            _users.TryRemove(username, out _);
        }

        public bool UsernameAlreadyExists(string username)
        {
            return _users.ContainsKey(username);
        }

        private string CreateConnectionId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}

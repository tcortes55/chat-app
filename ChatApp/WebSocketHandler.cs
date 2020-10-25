using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatApp
{
    public abstract class WebSocketHandler
    {
        protected ConnectionManager ConnectionManager { get; set; }

        public WebSocketHandler(ConnectionManager connectionManager)
        {
            ConnectionManager = connectionManager;
        }

        public virtual async Task OnConnected(WebSocket socket)
        {
            ConnectionManager.AddSocket(socket);
        }

        public virtual async Task<bool> ValidateConnection(WebSocket socket, string username)
        {
            if (ConnectionManager.UsernameAlreadyExists(username))
            {
                await ConnectionManager.RemoveSocket(ConnectionManager.GetId(socket), $"User {username} already exists");
                return false;
            }
            else
            {
                ConnectionManager.AddUser(socket, username);
                return true;
            }
        }

        public virtual async Task<string> OnDisconnected(WebSocket socket)
        {
            string socketId = ConnectionManager.GetId(socket);
            await ConnectionManager.RemoveSocket(socketId);

            string username = ConnectionManager.GetUsernameBySocketId(socketId);
            ConnectionManager.RemoveUser(username);

            return username;
        }

        public async Task SendMessageAsync(WebSocket socket, string message)
        {
            if (socket.State != WebSocketState.Open)
                return;

            await socket.SendAsync(buffer: new ArraySegment<byte>(array: Encoding.ASCII.GetBytes(message),
                                                                  offset: 0,
                                                                  count: message.Length),
                                   messageType: WebSocketMessageType.Text,
                                   endOfMessage: true,
                                   cancellationToken: CancellationToken.None);
        }

        public async Task SendMessageAsync(string socketId, string message)
        {
            await SendMessageAsync(ConnectionManager.GetSocketById(socketId), message);
        }

        public async Task SendMessageToAllAsync(string message)
        {
            foreach (var pair in ConnectionManager.GetAllSockets())
            {
                if (pair.Value.State == WebSocketState.Open)
                    await SendMessageAsync(pair.Value, message);
            }
        }

        public async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

            await SendMessageToAllAsync(message);
        }

        public string ReceiveString(WebSocketReceiveResult result, byte[] buffer)
        {
            return Encoding.UTF8.GetString(buffer, 0, result.Count);
        }

        public async Task BroadcastMessage(string message)
        {
            await SendMessageToAllAsync(message);
        }

        public List<string> GetAllUsers()
        {
            return ConnectionManager.GetAllUsernames();
        }

    }
}

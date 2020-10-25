using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ChatApp
{
    public class WebSocketMiddleware
    {
        //private static ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();
        //private static ConcurrentDictionary<string, string> _users = new ConcurrentDictionary<string, string>();

        private readonly RequestDelegate _next;
        private WebSocketHandler _webSocketHandler { get; set; }

        public WebSocketMiddleware(RequestDelegate next, WebSocketHandler webSocketHandler)
        {
            _next = next;
            _webSocketHandler = webSocketHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await _next.Invoke(context);
                return;
            }

            //CancellationToken cancellationToken = context.RequestAborted;
            WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
            await _webSocketHandler.OnConnected(socket);
            //var socketId = Guid.NewGuid().ToString();
            //var username = string.Empty;

            //_sockets.TryAdd(socketId, currentSocket);

            await Receive(socket, async (result, buffer) =>
            {
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var msg = _webSocketHandler.ReceiveString(result, buffer);

                    await HandleMessage(socket, msg);

                    return;
                }

                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await HandleDisconnect(socket);
                    return;
                }

            });

            while (false)
            {
                /*
                //if (cancellationToken.IsCancellationRequested)
                //{
                //    break;
                //}

                var response = await ReceiveMessageAsync(currentSocket, cancellationToken);

                if (string.IsNullOrEmpty(response))
                {
                    if (currentSocket.State != WebSocketState.Open)
                    {
                        break;
                    }

                    continue;
                }

                ClientMessage message = JsonSerializer.Deserialize<ClientMessage>(response);
                if (!message.IsValid())
                {
                    continue;
                }

                if (message.IsTypeConnection())
                {
                    if (_users.ContainsKey(message.Sender))
                    {
                        await CloseSocket(socketId, $"Username \"{message.Sender}\" already exists.", cancellationToken);
                    }
                    else
                    {
                        username = message.Sender;
                        _users.TryAdd(username, socketId);

                        ServerMessage connectMessage = new ServerMessage(
                            MessageType.CONNECTION,
                            $"User {username} joined the room.",
                            _users.Select(x => x.Key).ToList()
                            );

                        await SendMessageToAllAsync(JsonSerializer.Serialize(connectMessage), cancellationToken);
                    }
                }
                else if (message.IsTypeChat())
                {
                    string messageBody = message.BuildMessageBody();

                    ServerMessage serverMessage = new ServerMessage();
                    serverMessage.Type = MessageType.CHAT.ToString();
                    serverMessage.Content = messageBody;

                    await SendMessageToAllAsync(JsonSerializer.Serialize(serverMessage), cancellationToken);
                }
                */
            }

            //await CloseSocket(socketId, $"User {username} disconnected", cancellationToken, username);
            
            //ServerMessage disconnectMessage = new ServerMessage(
            //    MessageType.CONNECTION,
            //    $"User {username} left the room.",
            //    _users.Select(x => x.Key).ToList()
            //    );

            //await SendMessageToAllAsync(JsonSerializer.Serialize(disconnectMessage), cancellationToken);
        }

        private async Task HandleDisconnect(WebSocket socket)
        {
            string disconnectedUser = await _webSocketHandler.OnDisconnected(socket);

            ServerMessage disconnectMessage = new ServerMessage(disconnectedUser, true, _webSocketHandler.GetAllUsers());

            await _webSocketHandler.BroadcastMessage(JsonSerializer.Serialize(disconnectMessage));
        }

        private async Task HandleMessage(WebSocket socket, string message)
        {
            ClientMessage clientMessage = JsonSerializer.Deserialize<ClientMessage>(message);

            if (clientMessage.IsTypeConnection())
            {
                bool validate = await _webSocketHandler.ValidateConnection(socket, clientMessage.Sender);

                if (validate)
                {
                    ServerMessage connectMessage = new ServerMessage(clientMessage.Sender, false, _webSocketHandler.GetAllUsers());
                    await _webSocketHandler.BroadcastMessage(JsonSerializer.Serialize(connectMessage));
                }
            }
            else if (clientMessage.IsTypeChat())
            {
                ServerMessage chatMessage = new ServerMessage(clientMessage);
                await _webSocketHandler.BroadcastMessage(JsonSerializer.Serialize(chatMessage));
            }
        }

        private async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 4];

            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer),
                                                           cancellationToken: CancellationToken.None);

                    handleMessage(result, buffer);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                await HandleDisconnect(socket);
            }
        }

        private async Task CloseSocket(string socketId, string closingMessage, CancellationToken cancellationToken, string username = "")
        {
            //var socket = _sockets[socketId];

            //await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, closingMessage, cancellationToken);
            //socket.Dispose();

            //_sockets.TryRemove(socketId, out _);
            
            //if (!string.IsNullOrEmpty(username))
            //{
            //    _users.TryRemove(username, out _);
            //}
        }

        private async Task SendMessageToAllAsync(string data, CancellationToken ct = default(CancellationToken))
        {
            //foreach (var socket in _sockets)
            //{
            //    if (socket.Value.State != WebSocketState.Open)
            //    {
            //        continue;
            //    }

            //    await SendMessageAsync(socket.Value, data, ct);
            //}
        }

        private static Task SendMessageAsync(WebSocket socket, string data, CancellationToken ct = default(CancellationToken))
        {
            var buffer = Encoding.UTF8.GetBytes(data);
            var segment = new ArraySegment<byte>(buffer);
            return socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);
        }

        private static async Task<string> ReceiveMessageAsync(WebSocket socket, CancellationToken cancellationToken = default(CancellationToken))
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            using (var ms = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    result = await socket.ReceiveAsync(buffer, cancellationToken);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);
                if (result.MessageType != WebSocketMessageType.Text)
                {
                    return null;
                }

                // Encoding UTF8: https://tools.ietf.org/html/rfc6455#section-5.6
                using (var reader = new StreamReader(ms, Encoding.UTF8))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }
    }
}

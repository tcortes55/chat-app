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
        private static ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();
        private static ConcurrentDictionary<string, string> _users = new ConcurrentDictionary<string, string>();

        private readonly RequestDelegate _next;

        public WebSocketMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await _next.Invoke(context);
                return;
            }

            CancellationToken cancellationToken = context.RequestAborted;
            WebSocket currentSocket = await context.WebSockets.AcceptWebSocketAsync();
            var socketId = Guid.NewGuid().ToString();
            var username = string.Empty;

            _sockets.TryAdd(socketId, currentSocket);

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var response = await ReceiveMessageAsync(currentSocket, cancellationToken);

                if (string.IsNullOrEmpty(response))
                {
                    if (currentSocket.State != WebSocketState.Open)
                    {
                        break;
                    }

                    continue;
                }

                AppMessage message = JsonSerializer.Deserialize<AppMessage>(response);
                if (!message.IsValid())
                {
                    continue;
                }

                if (message.IsTypeConnection())
                {
                    if (_users.ContainsKey(message.Sender))
                    {
                        await currentSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Closing", cancellationToken);
                        currentSocket.Dispose();

                        _sockets.TryRemove(socketId, out _);
                    }
                    else
                    {
                        username = message.Sender;
                        _users.TryAdd(username, socketId);
                    }
                }
                else if (message.IsTypeChat())
                {
                    string messageBody = message.BuildMessageBody();

                    await SendMessageToAllAsync(messageBody, cancellationToken);
                }

            }

            _sockets.TryRemove(socketId, out _);

            if (!string.IsNullOrEmpty(username))
            {
                _users.TryRemove(username, out _);
            }

            await currentSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
            currentSocket.Dispose();
        }

        private async Task SendMessageToAllAsync(string data, CancellationToken ct = default(CancellationToken))
        {
            foreach (var socket in _sockets)
            {
                if (socket.Value.State != WebSocketState.Open)
                {
                    continue;
                }

                await SendMessageAsync(socket.Value, data, ct);
            }
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

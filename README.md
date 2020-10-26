# Chat App with WebSockets in ASP.NET Core (NO SignalR)
 
This is a chat app developed in order to demonstrate the use of WebSockets for a real-time application. It has a WebSocket middleware implemented in ASP.NET Core and a very simple client in HTML and vanilla JavaScript.

The application was developed in ASP.NET Core 3.1, using MS Visual Studio 2019. Only native libraries are used, since our goal is to demonstrate the low-level functioning of WebSockets. Most of the examples of WebSocket usage in ASP.NET Core found online use SignalR, but that is not the case in here.

### Basic functioning

A chat service consists in a server that allows simultaneous connection from different clients for message exchange. Before connecting, the user must choose a unique nickname and then enter a chat room, where they will be able to send and receive messages to other connected users.

### Solution description

The solution can be roughly divided in three main parts:

- ConnectionManager: keeps record of connected users and active sockets and deals with getting, adding and removing these records.

- WebSocketHandler: handles operations such as sending and receiving messages, and implements handling of connection and disconnection events.

- WebSocketMiddleware: when a WebSocket request is received, it accepts the connection and redirects the socket to OnConnected method from the handler. It validates that a unique username is being used (more on that later) and then awaits for new data as long as the socket is in the Open state.

### Implementation details

A very basic implementation of WebSockets in ASP.NET Core could have methods `AcceptWebSocketAsync`, `ReceiveAsync`, `SendAdync` and `CloseAsync`. A server would receive a WebSocket connection and answer the handshake, and then await for data to be received. This data could then be sent to connected users. The server could also receive a closing message, and then close the WebSocket.

For the chat app, we need to keep record of the connected users who should receive the incoming messages. In order to achieve this, we use `ConcurrentDictionary` in our `ConnectionManager`. A unique ID is atributed to each active socket.

#### Unique usernames

One of the requirements for this project is that usernames must be unique. There are many ways to implement this.

The way this requirement was implemented was quite simple, using the request query string. When a client sends a WebSocket request, the username is sent as in `ws://myserver/ws?username=MYUSERNAME`. The server then validates if `MYUSERNAME` is a valid username (unique, non-empty); if so, it is added to the collection of users/sockets, otherwise the socket is closed and the user receives the corresponding error message.

It is important to note that the query string should be used carefully. Sensitive data should not be exposed in query string, even if using HTTPS or WSS (content is encrypted but URL could be recorded in server logs, for example). We could use some other options in order to send data to the server:
- Upon completing the handshake, send data in a message as a callback to `socket.onopen`.
One way to achieve this: upon completing the handshake, the client could send the username (and possibly other data) in a message on `socket.onopen`. The server would then validate the data and, if not, it would close the socket.
- Separate responsibilities appropriately. Suppose our chat app requires authentication: instead of sending username and password in the WebSocket request query string, the credentials could be sent in an HTTP POST request to the server, who would then send a response with an access token. The token could then be sent in the WebSocket request query string, without risking exposing the credentials and avoiding mixing the authentication and the actual WebSocket handling.

#### Message structure

The solution has two classes representing two types of message: `ClientMessage` and `ServerMessage`.

`ClientMessage` is the message sent from the client to the server. It contains the follwing attributes:
- `Type`: it has two possible values, `CHAT` (indicates that it is a message to be sent to other users) OR `CONNECTION` (to be sent to the server on WebSocket opening; currently it is not being used but it was kept for future improvements).
- `Sender`: corresponds to the sender's username.
- `Receiver`: the user to whom the message is destined; if empty, the server assumes the recipient is "Everybody".
- `Content`: the actual message content, from user's input.
- `IsPrivate`: indicates whether the message should be sent in private to the Receiver (FOR FUTURE IMPLEMENTATION).

`ServerMessage` is the message sent from the server to the clients. It is triggered whenever a user connects, disconnects or sends a message. It contains the follwing attributes:
- `Type`: it has two possible values, `CHAT` (indicates that it is a chat message received from a user that is being sent to the other users) OR `CONNECTION` (indicates whether a user has entered or left the room).
- `Content`: the actual message content. In case this is a chat message, the content is built according to the original `ClientMessage`: `<Sender> to <Receiver: <Content>`. If it's a connection message, the content indicates the username and whether they entered or joined the room, e.g. `<Username> has left the room`.
- `Users`: returns a list of connected users. The client receives this list in order to enable a user to choose who should be the receiver of a message.

### References

- Microsoft documentation on WebSockets: [WebSockets support in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/websockets?view=aspnetcore-3.1)
- Microsoft documentation on Middlewares: [Write custom ASP.NET Core middleware
](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write?view=aspnetcore-3.1)
- Radu Matei's [Creating a WebSockets middleware for ASP .NET Core 3
](https://radu-matei.com/blog/aspnet-core-websockets-middleware/)

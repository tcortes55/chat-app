# Chat App
 
This is a chat app developed in order to demonstrate the use of WebSockets for a real-time application. It has a WebSocket middleware implemented in ASP.NET Core and a very simple client in HTML and vanilla JavaScript.

The application was developed in ASP.NET Core 3.1, using MS Visual Studio 2019. Only native libraries are used, since our goal is to demonstrate the low-level functioning of WebSockets. Most of the examples of WebSocket usage in ASP.NET Core found online use SignalR, but that is not the case in here.

### Basic functioning

A chat service consists in a server that allows simultaneous connection from different clients for message exchange. Before connecting, the user must choose a unique nickname and then enter a chat room, where they will be able to send and receive messages to other connected users.

### Solution description

The solution can be roughly divided in three main parts:

- ConnectionManager: keeps record of connected users and active sockets and deals with getting, adding and removing these records.

- WebSocketHandler: handles operations such as sending and receiving messages, and implements handling of connection and disconnection events.

- WebSocketMiddleware: when a WebSocket request is received, it accepts the connection and redirects the socket to OnConnected method from the handler. It validates that a unique username is being used (more on that later) and then awaits for new data as long as the socket is in the Open state.

### References

- Microsoft documentation on WebSockets: [WebSockets support in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/websockets?view=aspnetcore-3.1)
- Microsoft documentation on Middlewares: [Write custom ASP.NET Core middleware
](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write?view=aspnetcore-3.1)
- Radu Matei's [Creating a WebSockets middleware for ASP .NET Core 3
](https://radu-matei.com/blog/aspnet-core-websockets-middleware/)

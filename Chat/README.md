# Chat Server

Real-time chat server. Provides real-time message transmission and channel management features based on gRPC.

## 🌍 Language Selection

- [English](README.md) (Default)
- [한국어](README.ko.md)

## 🎯 Key Features

### Real-time Chat
- **gRPC Streaming**: Real-time bidirectional message transmission
- **Channel Management**: Public/private channel creation and management
- **Message Broadcasting**: Send messages to all users in a channel

### OAuth Authentication
- **Facebook Authentication**: Facebook OAuth support
- **GameCenter Authentication**: Apple GameCenter authentication support
- **External Authentication Providers**: Extensible authentication system

### Chat Features
- **Private Messages**: 1:1 private messaging
- **Group Chat**: Multi-user group chat
- **System Messages**: System notifications sent by server

## 🏗️ Architecture

### Core Components
- **ChatService**: gRPC chat service
- **ChannelManager**: Channel management
- **UserManager**: User management
- **MessageQueue**: Message queue management

### Communication Protocol
```protobuf
service ChatService {
    rpc JoinChannel(JoinChannelRequest) returns (JoinChannelResponse);
    rpc LeaveChannel(LeaveChannelRequest) returns (LeaveChannelResponse);
    rpc SendMessage(SendMessageRequest) returns (SendMessageResponse);
    rpc GetChannelMessages(GetChannelMessagesRequest) returns (GetChannelMessagesResponse);
    rpc StreamMessages(StreamMessagesRequest) returns (stream ChatMessage);
}
```

## 📊 Usage Examples

### Join Channel
```csharp
var request = new JoinChannelRequest {
    ChannelId = "general",
    UserId = "user123"
};

var response = await chatClient.JoinChannelAsync(request);
```

### Send Message
```csharp
var message = new SendMessageRequest {
    ChannelId = "general",
    UserId = "user123",
    Content = "Hello, World!",
    MessageType = MessageType.Text
};

var response = await chatClient.SendMessageAsync(message);
```

### Real-time Message Streaming
```csharp
using var call = chatClient.StreamMessages(new StreamMessagesRequest {
    ChannelId = "general",
    UserId = "user123"
});

await foreach (var message in call.ResponseStream) {
    Console.WriteLine($"{message.UserId}: {message.Content}");
}
```

## 📁 Project Structure

```
Chat/
├── Chat.csproj              # Project file
├── appsettings.json         # Default configuration
├── appsettings.Release.json # Production configuration
├── build.sh                 # Build script
├── GrpcHostedService.cs     # gRPC hosting service
├── Service/
│   └── ChatService.cs       # Chat service implementation
├── OAuth/                   # OAuth authentication
│   ├── ExternalProvider.cs
│   ├── ExternalProviderFacebook.cs
│   └── ExternalProviderGameCenter.cs
├── Credentials/             # Authentication credentials
│   └── README
└── bin/                     # Build output
```

## 🔧 Development Environment

### Requirements
- .NET 6.0 or higher
- gRPC
- Redis (optional, for caching)

### Dependencies
- gRPC communication
- OAuth authentication providers
- Message queue system

## 📈 Performance Optimization

### Message Processing
- **Asynchronous Processing**: Asynchronous message transmission processing
- **Batch Processing**: Simultaneous processing of multiple messages
- **Memory Optimization**: Efficient message structure

### Scalability
- **Horizontal Scaling**: Support for multiple server instances
- **Load Balancing**: gRPC load balancer support
- **Cache System**: Redis-based message cache

## 🔗 Related Projects

- **[Lobby/](../Lobby/README.md)** - Lobby server (user authentication)
- **[Battle/](../Battle/README.md)** - Battle server (in-game chat)
- **[Cache/](../Cache/README.md)** - Cache server
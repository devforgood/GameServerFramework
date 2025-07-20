# Lobby Server

Lobby server responsible for user authentication, matching, and all features except in-game functionality.

## 🌍 Language Selection

- [English](README.md) (Default)
- [한국어](README.ko.md)

## 🎯 Key Features

### User Authentication
- **Login Processing**: User authentication and session management
- **OAuth Support**: Support for external authentication providers like Facebook, GameCenter
- **Session Management**: User session creation and management

### Matching System
- **Periodic Matching Attempts**: Client polls at regular intervals, matching succeeds when conditions are met
- **Match Registration and Wait**: Client registers for matching and waits via Redis subscription, gRPC message stream
- **Redis Pub/Sub**: Notify waiting users via Redis pub when next client's matching attempt succeeds

### Game Management
- **Server Status Management**: Battle server status monitoring
- **Game Session Management**: Game session creation and management
- **Result Processing**: Game result storage and statistics

## 🏗️ Architecture

### Core Components
- **Authentication Service**: User authentication processing
- **Matching Service**: Matching logic processing
- **Game Session Manager**: Game session management
- **Redis Cache**: Session and matching data cache

### Matching Flow
1. **Match Request**: Client requests with matching criteria
2. **Match Search**: Search for other players matching criteria
3. **Match Success**: Notify when conditions are met
4. **Game Session Creation**: Request game session creation to battle server

## 📊 Matching Methods

### Periodic Matching Attempts
```csharp
// Client periodically requests matching
while (!isMatched) {
    var matchResult = await lobbyClient.RequestMatch(matchCriteria);
    if (matchResult.IsSuccess) {
        // Handle match success
        break;
    }
    await Task.Delay(1000); // Wait 1 second
}
```

### Match Registration and Wait
```csharp
// Register for matching
await lobbyClient.RegisterForMatch(matchCriteria);

// Wait via Redis subscription
using var subscription = redisClient.Subscribe("match_notifications");
await subscription.OnMessage(async (channel, message) => {
    // Handle match success notification
});
```

## 🚀 Usage Examples

### Server Startup
```bash
cd Lobby
dotnet run
```

### Configuration Files
- `appsettings.json`: Default configuration
- `appsettings.Release.json`: Production environment configuration

## 📁 Project Structure

```
Lobby/
├── Lobby.csproj           # Project file
├── appsettings.json       # Default configuration
├── appsettings.Release.json # Production configuration
├── GrpcHostedService.cs   # gRPC hosting service
├── Modules/               # Business logic modules
│   ├── Authentication/    # Authentication related
│   ├── Matching/          # Matching related
│   └── GameSession/       # Game session related
├── OAuth/                 # OAuth authentication
│   ├── ExternalProvider.cs
│   ├── ExternalProviderFacebook.cs
│   └── ExternalProviderGameCenter.cs
├── Service/               # Service layer
├── Query/                 # Data queries
├── WebAPIClient/          # Web API client
└── UnitTest/              # Unit tests
```

## 🔧 Development Environment

### Requirements
- .NET 6.0 or higher
- gRPC
- Redis
- Entity Framework Core

### Dependencies
- gRPC communication
- Redis cache
- OAuth authentication providers
- Communication with Battle server

## 📈 Monitoring

### Logging
- Structured logging support
- Matching success rate tracking
- Authentication failure logs

### Metrics
- Concurrent user count
- Matching wait time
- Matching success rate
- Authentication processing time

## 🔗 Related Projects

- **[Battle/](../Battle/README.md)** - Battle server (game sessions)
- **[Login/](../Login/README.md)** - Login server
- **[Cache/](../Cache/README.md)** - Cache server
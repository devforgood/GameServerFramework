# Battle Server

Battle server responsible for in-game character movement and skill synchronization.

## 🌍 Language Selection

- [English](README.md) (Default)
- [한국어](README.ko.md)

## 🎯 Key Features

### Character Movement Synchronization
- **Client Key Input Sync**: Queue sampled key values at regular intervals and send to server
- **Server Simulation**: Perform movement simulation upon receiving key input
- **Client Correction**: Correct with timestamp and coordinates from server, apply key inputs stored in queue after timestamp (movement rewind)

### Skill Synchronization
- **RPC Calls**: Synchronize unique information for each skill via RPC calls
- **Skill Effects**: Support various skill types including AoE, targeting, buff/debuff

### State Synchronization
- **Replication**: Periodically synchronize only objects with changed states in the world
- **Variable Replication**: Game object state synchronization

## 🏗️ Architecture

### Core Components
- **GridManager**: Spatial partitioning and entity management
- **Actor System**: Game entity management (characters, monsters, NPCs)
- **Skill System**: Skill definition and execution
- **Network Layer**: gRPC-based communication

### Synchronization Method
1. **Client → Server**: Key input, skill usage requests
2. **Server → Client**: Position updates, skill effects, state changes

## 📊 Performance Optimization

### Spatial Partitioning
- **GridManager**: SIMD-optimized spatial partitioning system
- **Line of Sight Range Search**: Efficient nearby entity search
- **AoE Masks**: Circular and sector range entity search

### Network Optimization
- **RUDP**: Secure latency without unnecessary TCP processing
- **Multiplexing**: Multiple purpose requests possible on single socket (channels)
- **QoS**: Packet loss guarantee

## 🚀 Usage Examples

### Server Startup
```bash
cd Battle
dotnet run
```

### Configuration Files
- `appsettings.json`: Default configuration
- `appsettings.Release.json`: Production environment configuration

## 📁 Project Structure

```
Battle/
├── Battle.csproj          # Project file
├── appsettings.json       # Default configuration
├── battle-server          # Executable file
├── Cache/                 # Cache related
│   ├── Cache.cs
│   ├── CacheThread.cs
│   └── Subscribe.cs
└── bin/                   # Build output
```

## 🔧 Development Environment

### Requirements
- .NET 6.0 or higher
- gRPC
- Redis (for caching)

### Dependencies
- gRPC communication
- GridManager (Engine project)
- GameData (data loading)

## 📈 Monitoring

### Logging
- Structured logging support
- Performance metric collection
- Error tracking

### Metrics
- Concurrent user count
- Packet processing rate
- Response time
- Skill usage frequency

## 🔗 Related Projects

- **[Engine/](../Engine/README.md)** - GridManager and spatial partitioning system
- **[Game/](../Game/README.md)** - Game logic (Actor, skill system)
- **[Lobby/](../Lobby/README.md)** - Matching and lobby server 
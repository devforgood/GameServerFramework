# Cache Server

High-performance cache server. Efficiently caches game data and session information based on Redis.

## 🌍 Language Selection

- [English](README.md) (Default)
- [한국어](README.ko.md)

## 🎯 Key Features

### Cache Management
- **Redis-based**: High-performance in-memory cache
- **Session Management**: User session information caching
- **Game Data Cache**: Skill, item, monster data caching

### Channel Management
- **Channel.cs**: Channel information management
- **ChannelLoader.cs**: Channel data loading
- **ChannelUpdater.cs**: Channel data updates

### Subscription System
- **Subscribe.cs**: Redis Pub/Sub subscription management
- **Real-time Updates**: Real-time notifications on data changes
- **Event Processing**: Cache event processing

## 🏗️ Architecture

### Core Components
- **Cache.cs**: Main cache manager
- **CacheThread.cs**: Cache processing thread
- **Subscribe.cs**: Subscription management
- **Channel/**: Channel-related components

### Cache Structure
```csharp
public class Cache {
    // Redis connection management
    private IConnectionMultiplexer redis;
    
    // Session cache
    private IDatabase sessionCache;
    
    // Game data cache
    private IDatabase gameDataCache;
    
    // Channel cache
    private IDatabase channelCache;
}
```

## 📊 Usage Examples

### Session Cache
```csharp
// Store session
await cache.SetSessionAsync(userId, sessionData);

// Retrieve session
var session = await cache.GetSessionAsync(userId);

// Remove session
await cache.RemoveSessionAsync(userId);
```

### Game Data Cache
```csharp
// Cache skill data
await cache.SetSkillDataAsync(skillId, skillData);

// Cache item data
await cache.SetItemDataAsync(itemId, itemData);

// Cache monster data
await cache.SetMonsterDataAsync(monsterId, monsterData);
```

### Channel Management
```csharp
// Load channel information
var channel = await channelLoader.LoadChannelAsync(channelId);

// Update channel information
await channelUpdater.UpdateChannelAsync(channelId, channelData);

// Subscribe to channel
await subscribe.SubscribeToChannelAsync(channelId, callback);
```

## 📁 Project Structure

```
Cache/
├── Cache.csproj             # Project file
├── Cache.cs                 # Main cache class
├── CacheThread.cs           # Cache processing thread
├── Subscribe.cs             # Subscription management
├── Channel/                 # Channel related
│   ├── Channel.cs           # Channel class
│   ├── ChannelLoader.cs     # Channel loader
│   └── ChannelUpdater.cs    # Channel updater
└── bin/                     # Build output
```

## 🔧 Development Environment

### Requirements
- .NET 6.0 or higher
- Redis server
- StackExchange.Redis

### Dependencies
- Redis client
- JSON serialization
- Asynchronous processing

## 📈 Performance Optimization

### Memory Management
- **LRU Cache**: Prioritize recently used data retention
- **Memory Compression**: Utilize Redis compression features
- **TTL Settings**: Automatic expiration time settings

### Processing Optimization
- **Asynchronous Processing**: All cache operations asynchronous processing
- **Batch Processing**: Simultaneous processing of multiple data
- **Connection Pooling**: Redis connection pool management

## 🔗 Related Projects

- **[Battle/](../Battle/README.md)** - Battle server (session cache)
- **[Lobby/](../Lobby/README.md)** - Lobby server (matching cache)
- **[Chat/](../Chat/README.md)** - Chat server (message cache)
- **[GameData/](../GameData/README.md)** - Game data (data cache) 
# Game Server Framework

A game server framework built with open-source technologies to accelerate game server development. Supports MMORPG, MORPG, PVP, Action, and other genres. Primary languages: C# and C++.

## 🌍 Language Selection

- [English](README.md) (Default)
- [한국어](README.ko.md)

## 🏗️ Project Structure

### 📁 Server Projects
- **[Battle/](Battle/README.md)** - Battle Server (In-game character movement and skill synchronization)
- **[Lobby/](Lobby/README.md)** - Lobby Server (User authentication, matching, etc.)
- **[Login/](Login/README.md)** - Login Server
- **[Chat/](Chat/README.md)** - Chat Server
- **[Cache/](Cache/README.md)** - Cache Server
- **[IAP/](IAP/README.md)** - In-App Purchase Server
- **[GmTool/](GmTool/README.md)** - GM Tools

### 📁 Core Libraries
- **[Engine/](Engine/README.md)** - Game Engine (GridManager, spatial partitioning system)
- **[BehaviorTree/](BehaviorTree/README.md)** - AI Behavior Tree
- **[FiniteStateMachine/](FiniteStateMachine/README.md)** - Finite State Machine
- **[Game/](Game/README.md)** - Game Logic (Actor, Item, Skill System)
- **[recastnavigation/](recastnavigation/README.md)** - Pathfinding System

### 📁 Data & Tools
- **[GameData/](GameData/README.md)** - Game Data (JSON-based)
- **[GameDataFlow/](GameDataFlow/README.md)** - Data Pipeline (JSON → Protobuf)
- **[GameDataProtobuf/](GameDataProtobuf/README.md)** - Protobuf Definitions
- **[SqlCodeGenerator/](SqlCodeGenerator/README.md)** - SQL Code Generator
- **[Models/](Models/README.md)** - Data Models

### 📁 Client
- **[Client/](Client/README.md)** - Unity Client (includes pathfinding test tools)

### 📁 External Libraries
- **[flatbuffer/](flatbuffer/README.md)** - FlatBuffers Serialization
- **[protos/](protos/README.md)** - gRPC Protocol Definitions

## 🚀 Quick Start

### Requirements
```bash
# Install dependencies via vcpkg
./vcpkg install behaviortree-cpp
./vcpkg install protobuf:x64-windows
```

### Key Dependencies
- [gRPC](https://github.com/grpc/grpc) - Communication Protocol
- [flatbuffers](https://github.com/google/flatbuffers) - Serialization
- [recastnavigation](https://github.com/recastnavigation/recastnavigation) - Pathfinding
- [lidgren](https://github.com/lidgren/lidgren-network-gen3) - Networking
- [Hazel-Networking](https://github.com/DarkRiftNetworking/Hazel-Networking) - Networking
- [BEPUPhysics](https://github.com/bepu/bepuphysics1) - Physics Engine
- [BehaviorTree.CPP](https://github.com/BehaviorTree/BehaviorTree.CPP) - AI Behavior Tree

## 🏛️ Server Architecture

![Server Architecture](https://user-images.githubusercontent.com/17477292/115057890-8e971280-9f1f-11eb-8043-6dbc64521900.png)

### Server Composition
1. **Lobby Server** - User authentication, matching, and all features except in-game
2. **Battle Server** - In-game character movement and skill synchronization
3. **AI Server** - AI state management, pathfinding

### In-Game Synchronization
1. **Character Movement Sync** - Client key input → Server simulation → Client correction
2. **State Synchronization** - Periodic sync of only changed objects (replication)
3. **Skill Synchronization** - Skill information sync via RPC calls

### Protocols
- **gRPC** - Stateless protocol suitable for mobile environments
- **RUDP** - Secures latency without unnecessary TCP processing

## 📊 Sequence Diagrams

### Login Sequence
![Login Sequence](https://user-images.githubusercontent.com/17477292/115049395-a4073f00-9f15-11eb-9a40-04d1922dec97.png)

### Game Result Sequence
![Game Result Sequence](https://user-images.githubusercontent.com/17477292/115050008-4a534480-9f16-11eb-9a40-04d1922dec97.png)

### Matching Sequence
![Match Sequence](https://user-images.githubusercontent.com/17477292/115050031-50492580-9f16-11eb-80f7-c55eae32d863.png)

### Matching v2 Sequence
![Match v2 Sequence](https://user-images.githubusercontent.com/17477292/115050025-4e7f6200-9f16-11eb-80f7-c55eae32d863.png)

## 🛠️ Development Tools

### Unity Client Tools
- **Sample Terrain Generator** - Terrain generation for pathfinding tests
- **Advanced Terrain Generator** - Advanced terrain generation (various test scenarios)
- **Pathfinding Test Manager** - Pathfinding agent management

### Data Pipeline
- **JSON → Protobuf Conversion** - Fast loading and automatic code generation
- **Factory Pattern Auto-Generation** - ID-based object creation code automation
- **Reusable Property Structure** - Component-based design

## 📈 Performance Optimization

### GridManager
- **SIMD Vectorization** - Simultaneous calculation of 8 distances using AVX2 instructions
- **Trigonometric Look-up Tables** - Fast angle calculation using pre-computed values
- **Adaptive Grid Structure** - Automatic Vector/HashMap selection based on data density

### Processing Performance
- **10,000 entities**: AoE queries < 1ms
- **100,000 entities**: Line of sight range search < 5ms
- **SIMD optimization**: 3-8x performance improvement over conventional methods

## 🤝 Contributing

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is distributed under the MIT License. See the `LICENSE` file for details.

## 📞 Contact

Project Link: [https://github.com/yourusername/GameServerFramework](https://github.com/yourusername/GameServerFramework)


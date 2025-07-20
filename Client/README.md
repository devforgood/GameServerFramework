# Unity Client

Unity-based game client. Includes pathfinding test tools and various development utilities.

## 🌍 Language Selection

- [English](README.md) (Default)
- [한국어](README.ko.md)

## 🎯 Key Features

### Pathfinding Test Tools
- **Sample Terrain Generator**: Basic terrain generation tool
- **Advanced Terrain Generator**: Advanced terrain generation (various test scenarios)
- **Pathfinding Test Manager**: Pathfinding agent management

### Terrain Generation Types
- **SimplePlane**: Basic plane terrain
- **Maze**: Maze-like terrain
- **City**: City-like terrain (buildings, bridges)
- **Forest**: Forest-like terrain (trees, elevation changes)
- **Mountain**: Mountainous terrain (mountains, tunnels)
- **Battlefield**: Battlefield terrain (trenches, bunkers)
- **Dungeon**: Dungeon terrain (walls, pillars)
- **Custom**: User-defined terrain

### Pathfinding Test Scenarios
- **SimplePathfinding**: Basic pathfinding test
- **ComplexMaze**: Complex maze test
- **MultiLevel**: Multi-level structure test
- **DynamicObstacles**: Dynamic obstacles test
- **PerformanceTest**: Performance test

## 🛠️ Development Tools

### Sample Terrain Generator
```
Tools > Sample Terrain Generator
```
- 6 basic terrain types
- Obstacles, slopes, water generation
- Configurable parameters

### Advanced Terrain Generator
```
Tools > Advanced Terrain Generator
```
- 8 advanced terrain types
- 5 test scenarios
- Performance optimization settings
- Visualization options

### Pathfinding Test Manager
```
Tools > Pathfinding Test Manager
```
- Agent creation and management
- Real-time path visualization
- Target randomization
- Performance monitoring

## 🚀 Usage Guide

### 1. Terrain Generation
1. Select `Tools > Sample Terrain Generator` or `Tools > Advanced Terrain Generator` in Unity Editor
2. Choose desired terrain type and settings
3. Click "Generate Sample Terrain" or "Generate Advanced Terrain" button

### 2. Pathfinding Test
1. Select `Tools > Pathfinding Test Manager`
2. Adjust agent count and settings
3. Click "Spawn Agents" button to create agents
4. Verify automatic pathfinding behavior in Play mode

### 3. Agent Management
- **Spawn Agents**: Create new agents
- **Clear All Agents**: Remove all agents
- **Randomize Targets**: Randomize targets for all agents
- **Stop All Agents**: Stop all agents

## 📁 Project Structure

```
Client/
├── Assets/
│   ├── Scripts/
│   │   ├── Editor/
│   │   │   ├── SampleTerrainGenerator.cs      # Basic terrain generator
│   │   │   ├── AdvancedTerrainGenerator.cs    # Advanced terrain generator
│   │   │   └── PathfindingTestManager.cs      # Pathfinding test manager
│   │   ├── PathfindingAgent.cs                # Pathfinding agent
│   │   ├── GameManager.cs                     # Game manager
│   │   ├── Session.cs                         # Network session
│   │   └── PacketFactory.cs                   # Packet factory
│   ├── Resources/
│   │   └── Materials/                         # Materials
│   │       ├── TerrainMaterial.mat            # Terrain material
│   │       ├── ObstacleMaterial.mat           # Obstacle material
│   │       └── WaterMaterial.mat              # Water material
│   ├── Scenes/                               # Unity scenes
│   ├── Prefabs/                              # Prefabs
│   └── GeneratedNavMeshes/                   # Generated navmeshes
├── Packages/                                 # Unity packages
├── ProjectSettings/                          # Project settings
└── Library/                                  # Unity library
```

## 🔧 Development Environment

### Requirements
- Unity 2021.3 LTS or higher
- .NET 4.x or .NET Standard 2.1
- Visual Studio 2019 or higher (for C# script editing)

### Dependencies
- **RecastNavigation**: Pathfinding system
- **SharpNav**: Navigation mesh
- **FlatBuffers**: Serialization
- **gRPC**: Network communication

## 📊 Performance Optimization

### Terrain Generation Optimization
- **Static Batching**: Static object batching
- **LOD System**: Level of detail based on distance
- **Memory Optimization**: Efficient memory usage

### Pathfinding Optimization
- **Path Caching**: Cache frequently used paths
- **Batch Processing**: Simultaneous processing of multiple agents
- **Visualization Optimization**: Improved path display performance

## 🎮 Game Features

### Network Communication
- **Session.cs**: Network session management
- **PacketFactory.cs**: Packet creation and parsing
- **gRPC Communication**: Real-time communication with server

### Game Data
- **GameManager.cs**: Game state management
- **ItemFactory.cs**: Item creation
- **SkillFactory.cs**: Skill creation

### UI System
- **In-game UI**: Player status, skill bar
- **Lobby UI**: Matching, friend list
- **Settings UI**: Graphics, sound settings

## 🔗 Related Projects

- **[Battle/](../Battle/README.md)** - Battle server (in-game synchronization)
- **[Lobby/](../Lobby/README.md)** - Lobby server (matching, authentication)
- **[Engine/](../Engine/README.md)** - Game engine (GridManager)
- **[GameData/](../GameData/README.md)** - Game data

## 📈 Development Guide

### Adding New Terrain Types
1. Add new `TerrainType` in `AdvancedTerrainGenerator.cs`
2. Implement terrain generation method
3. Add new option to UI

### Adding New Pathfinding Tests
1. Add new type to `PathfindingTestType` enum
2. Add logic to `ConfigureTerrainForTestType` method
3. Implement test scenario

### Adding Custom Agents
1. Inherit from `PathfindingAgent` class
2. Implement custom behavior logic
3. Integrate with `PathfindingTestManager`

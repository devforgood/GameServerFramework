# Engine - High-Performance Game Server Engine

A modular, high-performance game server engine. Built around a SIMD-optimized `GridManager` spatial-partitioning core, it also provides an ECS, an event broker, actor/AI systems, and player progression (item · skill · quest · level) backed by SQL persistence. Code is organized by domain into per-module folders that mirror the Visual Studio project filters.

## 🌍 Language Selection

- [English](README.md) (Default)
- [한국어](README.ko.md)

## 🎯 Key Features

### Spatial Partitioning and Management
- **2D Grid-based Spatial Partitioning**: Efficient spatial queries by dividing world into cells
- **Adaptive Grid Structure**: Automatic selection between Vector-based or HashMap-based grid depending on entity count
- **Type-based Entity Separation**: Separate management of characters and monsters for performance optimization

### Advanced Spatial Queries
- **Line of Sight Range Search**: Efficiently search for other entities around specific entities
- **AoE (Area of Effect) Masks**: Search entities within circular and sector ranges
- **Real-time Position Updates**: Automatic grid cell updates when entities move

### Performance Optimization
- **SIMD Vectorization**: Simultaneous calculation of 8 distances using AVX2 instructions
- **Trigonometric Look-up Tables**: Fast angle calculation using pre-computed trigonometric values
- **Batch Processing**: Process large numbers of entities in batches for improved cache efficiency

### Game Systems
- **ECS**: Entity-Component-System with cache-optimized systems and a system manager
- **Event-Driven Architecture**: Event broker/bus with thread-safe and lock-free (Boost/Moodycamel) queues
- **Actor & AI**: Actor/Character/Monster entities driven by Behavior Trees (with BT debugging and Lua scripting)
- **Player Progression**: Item, skill, quest, and **level** systems with data load/save
- **Persistence**: SQL client with generated DAO/VO and change tracking

## 🏗️ Architecture

### Core Class Structure

```cpp
class GridManager {
    // Main grid manager
    // - Spatial partitioning and entity management
    // - High-performance spatial queries
};

class IGrid {
    // Grid interface
    // - Vector-based: Optimized for dense data
    // - HashMap-based: Optimized for sparse data
};

struct Cell {
    std::unordered_set<IGridActor*> characters;  // Character entities
    std::unordered_set<IGridActor*> monsters;    // Monster entities
};
```

### Adaptive Grid Selection Logic

```cpp
GridManager::GridManager(int width, int height, int cellSize) {
    if (width * height > 100000) {
        grid_ = new GridHashMap(width, height, cellSize);  // Large maps
    } else {
        grid_ = new GridVector(width, height, cellSize);   // Small maps
    }
}
```

## 🚀 Performance Optimization Techniques

### 1. SIMD Vectorization (AVX2)
```cpp
inline void calculateDistancesSIMD(const float* x_coords, const float* y_coords, 
                                 float center_x, float center_y, float* distances_sq, 
                                 int count) {
    // Calculate 8 distances simultaneously
    __m256 centerX = _mm256_set1_ps(center_x);
    __m256 centerY = _mm256_set1_ps(center_y);
    
    for (int i = 0; i < count; i += 8) {
        __m256 x = _mm256_loadu_ps(&x_coords[i]);
        __m256 y = _mm256_loadu_ps(&y_coords[i]);
        
        __m256 dx = _mm256_sub_ps(x, centerX);
        __m256 dy = _mm256_sub_ps(y, centerY);
        
        __m256 distSq = _mm256_add_ps(
            _mm256_mul_ps(dx, dx),
            _mm256_mul_ps(dy, dy)
        );
        
        _mm256_storeu_ps(&distances_sq[i], distSq);
    }
}
```

### 2. Trigonometric Look-up Tables
```cpp
class TrigLookupTable {
private:
    static constexpr size_t SAMPLES_PER_QUADRANT = 64;  // 64 samples per 90 degrees
    static constexpr size_t TABLE_SIZE = SAMPLES_PER_QUADRANT * 4;  // Full 360 degrees
    alignas(32) std::array<float, TABLE_SIZE> sin_table;
    alignas(32) std::array<float, TABLE_SIZE> cos_table;
    
public:
    float sin(float angle) const;  // Fast sine calculation using linear interpolation
    float cos(float angle) const;  // Fast cosine calculation using linear interpolation
};
```

### 3. Batch Processing System
```cpp
static constexpr size_t MAX_BATCH_SIZE = 256;  // Maximum entities to process at once
alignas(32) float x_coords[MAX_BATCH_SIZE];    // 32-byte aligned coordinate arrays
alignas(32) float y_coords[MAX_BATCH_SIZE];
alignas(32) float distances_sq[MAX_BATCH_SIZE];
```

## 📊 Usage Examples

### Basic Usage
```cpp
// Initialize grid manager (1000x1000 world, 10x10 cell size)
GridManager gridManager(1000, 1000, 10);

// Add entity
IGridActor* player = new Player(100.0f, 200.0f);
gridManager.add(player);

// Move entity
gridManager.move(player, 150.0f, 250.0f);

// Search entities in line of sight range
auto nearbyEntities = gridManager.getEntitiesInViewRange(player, 50.0f);
```

### AoE Skill Implementation
```cpp
// Circular AoE (radius 30, center point (100, 100))
auto entitiesInCircle = gridManager.getEntitiesInAoEMask(100.0f, 100.0f, 30.0f, 0.0f);

// Sector AoE (radius 30, direction 45 degrees, angle 90 degrees)
auto entitiesInSector = gridManager.getEntitiesInAoEMask(100.0f, 100.0f, 30.0f, 45.0f, 90.0f);
```

### Broadcast Messages
```cpp
// Send message to all entities within specific range
gridManager.broadcastToNearby(100.0f, 100.0f, 50.0f, "Hello World!");
```

## 🔧 System Requirements

### Compiler Requirements
- **C++17** or higher support
- **AVX2** instruction set support (Intel Haswell or higher, AMD Excavator or higher)

### Header Files
```cpp
#include <immintrin.h>  // SIMD operations
#include <array>        // Fixed-size arrays
#include <unordered_set> // Hash sets
#include <vector>       // Dynamic arrays
```

## 📈 Performance Benchmarks

### Processing Performance
- **10,000 entities**: AoE queries < 1ms
- **100,000 entities**: Line of sight range search < 5ms
- **SIMD optimization**: 3-8x performance improvement over conventional methods

### Memory Usage
- **Vector-based**: Optimized for dense data, low memory overhead
- **HashMap-based**: Optimized for sparse data, dynamic memory allocation

## 📁 Project Structure

Sources are grouped by domain into folders that mirror the Visual Studio project filters. Files use flat `#include "Name.h"` includes; every module folder (plus the project root) is registered in `AdditionalIncludeDirectories`, so headers resolve regardless of their physical folder.

```
Engine/
├── Engine.vcxproj / .filters   # Visual Studio project & filters
├── Grid/                       # Spatial partitioning (GridManager, IGridActor)
├── ECS/                        # Entity-Component-System (ECS, Components, Systems, SystemManager)
├── GameObject/                 # GameObject base, Component, ComponentTypeId
├── Actor/                      # Actor / Character / Monster (+ ActorFactory)
├── AI/                         # Behavior Tree AI (BehaviorTreeCPP, MonsterBT, BTDebug, LuaObject)
├── Controller/                 # PlayerController
├── Player/                     # Player + data load/save, item, skill, quest, level, event broker
├── Level/                      # Leveling system (Level, LevelFactory)
├── Quest/                      # Quest system (Main / Repeated / LimitedTime + QuestFactory)
├── skill/                      # Skill system (Skill, Jump/NormalAttack, SkillManager + SkillFactory)
├── MonsterData/                # Monster data tables (+ MonsterDataFactory)
├── GameMode/                   # GameMode (+ GameModeFactory)
├── World/                      # World, Map, NavMap
├── Movement/                   # Vector3, Walker
├── EventBroker/                # Event bus/queue (thread-safe & lock-free: Boost/Moodycamel)
├── EventMessage/               # Event message definitions
├── Message/                    # Game messages (GameMessage, SendMessage)
├── SQL/                        # DB persistence (SqlClient, DbRecord, generated/ DAO & VO)
├── GameData/                   # Game data resource loading (ResourceLoader, gamedata)
├── Random/ · RingBuffer/ · Time/   # Utilities (RandomUtil, RingBuffer, TimeStamp)
├── flatbuffers/                # Generated FlatBuffers schema (syncnet_generated)
├── (root)                      # Common, Server, Item/ItemFactory, MapFactory, helpers
└── x64/Debug/                  # Build output
```

## 🔗 Related Projects

- **[Battle/](../Battle/README.md)** - Battle server (uses GridManager)
- **[Game/](../Game/README.md)** - Game logic (Actor system)
- **[BehaviorTree/](../BehaviorTree/README.md)** - AI behavior tree 
# Entity Component System (ECS) - Cache-Optimized Implementation

## Overview

This ECS implementation is designed to maximize CPU cache hit rates by using data-oriented design principles. The system processes components in packed arrays, ensuring optimal memory access patterns for high-performance game development.

## Key Features

### 1. Cache-Friendly Design
- **Packed Arrays**: Components of the same type are stored in contiguous memory
- **Array-Based Processing**: Systems iterate over component arrays directly
- **Primitive Types Only**: All component members are primitive types for optimal cache alignment
- **No Virtual Functions**: Eliminates vtable lookups during processing

### 2. Memory Layout
```
ComponentArray<PositionComponent>:
[Entity0_Position][Entity1_Position][Entity2_Position]...

ComponentArray<VelocityComponent>:
[Entity0_Velocity][Entity1_Velocity][Entity2_Velocity]...
```

### 3. System Processing
Systems process components in parallel arrays, maximizing cache locality:
```cpp
// Cache-friendly iteration
for (size_t i = 0; i < minSize; ++i)
{
    // All components at index i are likely in the same cache line
    auto& position = positions[i];
    auto& velocity = velocities[i];
    // Process...
}
```

## Architecture

### Core Classes

#### EntityManager
- Manages entity lifecycle (create/destroy)
- Handles component registration and storage
- Provides component array access

#### ComponentArray<T>
- Stores components of type T in contiguous memory
- Maintains entity-to-index mapping
- Provides direct array access for cache optimization

#### System<T...>
- Template-based system that processes specific component combinations
- Uses lambda functions for update logic
- Automatically handles component array iteration

#### SystemManager
- Manages all systems
- Provides unified update interface
- Handles system registration

## Usage Example

### 1. Register Components
```cpp
auto& entityManager = systemManager.GetEntityManager();
entityManager.RegisterComponent<PositionComponent>();
entityManager.RegisterComponent<VelocityComponent>();
entityManager.RegisterComponent<HealthComponent>();
```

### 2. Create Systems
```cpp
systemManager.RegisterSystem<PositionComponent, VelocityComponent>(
    [](float deltaTime, PositionComponent& position, VelocityComponent& velocity) {
        // Update position based on velocity
        position.x += velocity.vx * deltaTime;
        position.y += velocity.vy * deltaTime;
        position.z += velocity.vz * deltaTime;
    });
```

### 3. Create Entities
```cpp
EntityID player = entityManager.CreateEntity();
entityManager.AddComponent(player, PositionComponent{0.0f, 0.0f, 0.0f});
entityManager.AddComponent(player, VelocityComponent{0.0f, 0.0f, 0.0f});
entityManager.AddComponent(player, HealthComponent{100.0f, 100.0f, true});
```

### 4. Game Loop
```cpp
while (gameRunning)
{
    float deltaTime = GetDeltaTime();
    systemManager.Update(deltaTime);
}
```

## Performance Benefits

### 1. Cache Locality
- Components are stored in packed arrays
- Sequential memory access patterns
- Reduced cache misses during iteration

### 2. Memory Efficiency
- No padding between components of the same type
- Optimal memory alignment for primitive types
- Reduced memory fragmentation

### 3. Processing Speed
- Direct array access without indirection
- No virtual function calls during processing
- SIMD-friendly data layout

## Component Design Guidelines

### 1. Use Only Primitive Types
```cpp
// Good - All primitive types
struct PositionComponent
{
    float x, y, z;
};

// Bad - Contains non-primitive types
struct BadComponent
{
    std::string name;  // Non-primitive
    std::vector<int> data;  // Non-primitive
};
```

### 2. Keep Components Small
- Smaller components fit better in cache lines
- Aim for 64 bytes or less per component
- Use multiple small components instead of one large component

### 3. Align Data Properly
```cpp
struct AlignedComponent
{
    float x, y, z;     // 12 bytes
    uint32_t flags;    // 4 bytes
    // Total: 16 bytes (good alignment)
};
```

## System Design Guidelines

### 1. Process Related Components Together
```cpp
// Good - Processes related components
systemManager.RegisterSystem<PositionComponent, VelocityComponent>(
    MovementSystem::Update);

// Bad - Processes unrelated components
systemManager.RegisterSystem<PositionComponent, AudioComponent>(
    SomeSystem::Update);
```

### 2. Minimize Component Dependencies
- Systems should require minimal component combinations
- Avoid systems that need many different component types
- Use multiple small systems instead of one large system

### 3. Use Efficient Algorithms
```cpp
// Good - Simple, cache-friendly processing
for (size_t i = 0; i < size; ++i)
{
    positions[i].x += velocities[i].vx * deltaTime;
}

// Bad - Complex processing with many branches
for (size_t i = 0; i < size; ++i)
{
    if (complex_condition(positions[i], velocities[i]))
    {
        // Complex processing...
    }
}
```

## Advanced Features

### 1. Direct Array Access
```cpp
auto* positionArray = entityManager.GetComponentArray<PositionComponent>();
PositionComponent* positions = positionArray->GetArray();
size_t size = positionArray->GetSize();

// Direct access for maximum performance
for (size_t i = 0; i < size; ++i)
{
    ProcessPosition(positions[i]);
}
```

### 2. Component Queries
```cpp
// Get all entities with specific components
auto entities = entityManager.GetEntitiesWithComponents<
    PositionComponent, VelocityComponent, HealthComponent>();
```

### 3. Component Removal
```cpp
// Remove components from entities
entityManager.RemoveComponent<VelocityComponent>(entityID);
```

## Performance Monitoring

### 1. Component Array Statistics
```cpp
auto* array = entityManager.GetComponentArray<PositionComponent>();
std::cout << "Position components: " << array->GetSize() << std::endl;
```

### 2. System Performance
```cpp
auto start = std::chrono::high_resolution_clock::now();
systemManager.Update(deltaTime);
auto end = std::chrono::high_resolution_clock::now();
auto duration = std::chrono::duration_cast<std::chrono::microseconds>(end - start);
std::cout << "Update time: " << duration.count() << " microseconds" << std::endl;
```

## Best Practices

1. **Profile First**: Always profile your specific use case
2. **Measure Cache Performance**: Use tools like Intel VTune or AMD μProf
3. **Test with Real Data**: Use realistic entity counts and component combinations
4. **Optimize Hot Paths**: Focus optimization on frequently executed systems
5. **Keep It Simple**: Avoid premature optimization

## Conclusion

This ECS implementation provides a solid foundation for high-performance game development by prioritizing cache efficiency and data locality. The template-based design ensures type safety while maintaining excellent performance characteristics. 
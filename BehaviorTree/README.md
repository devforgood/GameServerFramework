# Behavior Tree

AI behavior tree system. Represents complex AI logic in tree structure to provide maintainability and extensibility.

## 🌍 Language Selection

- [English](README.md) (Default)
- [한국어](README.ko.md)

## 🎯 Key Features

### Behavior Tree Node Types
- **Action Node**: Node that performs actual actions
- **Condition Node**: Node that checks conditions
- **Composite Node**: Node that manages multiple child nodes
  - **Sequence**: Succeeds only if all children succeed
  - **Selector**: Succeeds if any child succeeds
  - **Parallel**: Executes all children simultaneously

### AI Patterns
- **Sequential Execution**: Perform actions in specific order
- **Conditional Execution**: Select different actions based on conditions
- **Parallel Execution**: Perform multiple actions simultaneously
- **Priority-based**: Select actions based on priority

## 🏗️ Architecture

### Core Class Structure

```cpp
class BehaviorTree {
    // Main behavior tree class
    // - Tree structure management
    // - Node execution and result processing
};

class BehaviorNode {
    // Base class for all nodes
    // - Execution state management
    // - Child node management
};

class ActionNode : public BehaviorNode {
    // Node that performs actual actions
    // - Movement, attack, item usage, etc.
};

class ConditionNode : public BehaviorNode {
    // Node that checks conditions
    // - HP check, distance check, status check, etc.
};
```

### Node Execution States
- **SUCCESS**: Node completed successfully
- **FAILURE**: Node failed
- **RUNNING**: Node is executing
- **IDLE**: Node is waiting

## 📊 Usage Examples

### Basic Behavior Tree Construction
```cpp
// Basic behavior tree for AI monster
auto root = std::make_unique<SelectorNode>();

// 1. Check if can attack and attack
auto attackSequence = std::make_unique<SequenceNode>();
attackSequence->addChild(std::make_unique<CanAttackCondition>());
attackSequence->addChild(std::make_unique<AttackAction>());
root->addChild(std::move(attackSequence));

// 2. Check if player is in sight and chase
auto chaseSequence = std::make_unique<SequenceNode>();
chaseSequence->addChild(std::make_unique<PlayerInSightCondition>());
chaseSequence->addChild(std::make_unique<ChaseAction>());
root->addChild(std::move(chaseSequence));

// 3. Default behavior (patrol)
auto patrolSequence = std::make_unique<SequenceNode>();
patrolSequence->addChild(std::make_unique<PatrolAction>());
root->addChild(std::move(patrolSequence));
```

### Condition Node Example
```cpp
class CanAttackCondition : public ConditionNode {
public:
    Status execute() override {
        auto target = getTarget();
        if (!target) return Status::FAILURE;
        
        float distance = calculateDistance(getPosition(), target->getPosition());
        if (distance <= attackRange) {
            return Status::SUCCESS;
        }
        return Status::FAILURE;
    }
};
```

### Action Node Example
```cpp
class AttackAction : public ActionNode {
public:
    Status execute() override {
        auto target = getTarget();
        if (!target) return Status::FAILURE;
        
        // Perform attack logic
        performAttack(target);
        return Status::SUCCESS;
    }
};
```

## 🔧 System Requirements

### Compiler Requirements
- **C++17** or higher support
- **STL** container usage

### Header Files
```cpp
#include <memory>       // Smart pointers
#include <vector>       // Dynamic arrays
#include <functional>   // Function objects
```

## 📈 Performance Optimization

### Memory Management
- **Smart Pointers**: Automatic memory management
- **Node Pooling**: Reuse frequently used nodes
- **Cache-friendly**: Optimized node data structures

### Execution Optimization
- **Early Termination**: Immediate termination on condition failure
- **Parallel Execution**: Simultaneous execution of independent nodes
- **Priority-based**: Prioritize important actions

## 📁 Project Structure

```
BehaviorTree/
├── BehaviorTree.vcxproj     # Visual Studio project file
├── BehaviorTree.vcxproj.filters # Project filters
├── Behavior.h               # Behavior tree header
├── Behavior.cpp             # Behavior tree implementation
├── BehaviorEvent.h          # Behavior event header
├── BehaviorEvent.cpp        # Behavior event implementation
├── Nodes/                   # Node classes
│   ├── ActionNode.h         # Action node
│   ├── ConditionNode.h      # Condition node
│   ├── CompositeNode.h      # Composite node
│   └── DecoratorNode.h      # Decorator node
└── x64/                     # Build output
    └── Debug/
```

## 🎮 AI Pattern Examples

### Monster AI
```cpp
// Monster behavior tree
auto monsterAI = std::make_unique<SelectorNode>();

// 1. Flee if HP is low
auto fleeSequence = std::make_unique<SequenceNode>();
fleeSequence->addChild(std::make_unique<LowHPCondition>());
fleeSequence->addChild(std::make_unique<FleeAction>());
monsterAI->addChild(std::move(fleeSequence));

// 2. Attack if possible
auto attackSequence = std::make_unique<SequenceNode>();
attackSequence->addChild(std::make_unique<CanAttackCondition>());
attackSequence->addChild(std::make_unique<AttackAction>());
monsterAI->addChild(std::move(attackSequence));

// 3. Chase player
auto chaseSequence = std::make_unique<SequenceNode>();
chaseSequence->addChild(std::make_unique<PlayerInSightCondition>());
chaseSequence->addChild(std::make_unique<ChaseAction>());
monsterAI->addChild(std::move(chaseSequence));

// 4. Patrol
auto patrolSequence = std::make_unique<SequenceNode>();
patrolSequence->addChild(std::make_unique<PatrolAction>());
monsterAI->addChild(std::move(patrolSequence));
```

### NPC AI
```cpp
// NPC behavior tree
auto npcAI = std::make_unique<ParallelNode>();

// 1. Check for conversable players (parallel execution)
npcAI->addChild(std::make_unique<CheckConversationAction>());

// 2. Play default animation (parallel execution)
npcAI->addChild(std::make_unique<PlayIdleAnimationAction>());

// 3. React to environment (parallel execution)
npcAI->addChild(std::make_unique<ReactToEnvironmentAction>());
```

## 🔗 Related Projects

- **[Game/](../Game/README.md)** - Game logic (AI entities)
- **[Engine/](../Engine/README.md)** - Game engine (spatial partitioning)
- **[Battle/](../Battle/README.md)** - Battle server (AI synchronization)
- **[FiniteStateMachine/](../FiniteStateMachine/README.md)** - Finite state machine 
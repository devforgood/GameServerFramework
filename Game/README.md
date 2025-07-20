# Game Logic

C++ project responsible for core game logic. Includes Actor system, item system, skill system, and more.

## 🌍 Language Selection

- [English](README.md) (Default)
- [한국어](README.ko.md)

## 🎯 Key Features

### Actor System
- **Actor.h**: Base class for all game entities
- **Character Management**: Players, NPCs, monsters, etc.
- **State Management**: HP, MP, level, experience, etc.
- **Position Management**: World coordinates and movement

### Item System
- **BaseItem.h**: Base class for items
- **Item Types**: Weapons, armor, consumables, etc.
- **Item Effects**: Stat increases, buffs, debuffs, etc.
- **Inventory Management**: Item storage and management

### Skill System
- **Skill Definition**: Skill effects, cooldowns, ranges, etc.
- **Skill Execution**: Skill usage and effect application
- **Skill Types**: Attack, buff, debuff, movement, etc.
- **Skill Levels**: Skill enhancement and leveling

### Data Management
- **GameData/**: Binary game data
- **SQL/**: Database schema and queries
- **Logging System**: Game event logging

## 🏗️ Architecture

### Core Class Structure

```cpp
class Actor {
    // Base class for all game entities
    // - Position, state, attribute management
    // - Event processing
};

class BaseItem {
    // Base class for items
    // - Item attributes, effect management
    // - Use, equip, unequip
};

class Skill {
    // Base class for skills
    // - Skill effects, cooldown management
    // - Targeting, range processing
};
```

### Data Structure
```
Game/
├── Actor.cpp                # Actor implementation
├── Actor.h                  # Actor header
├── BaseItem.h               # Item base class
├── GameData/                # Game data
│   ├── item.bytes           # Item binary data
│   └── skill.bytes          # Skill binary data
├── SQL/                     # Database
│   └── generated/           # Generated SQL files
└── logs/                    # Game logs
```

## 📊 Usage Examples

### Actor Creation and Management
```cpp
// Create player
auto player = std::make_unique<Player>();
player->setPosition(100.0f, 200.0f);
player->setLevel(10);
player->setHP(100);

// Create monster
auto monster = std::make_unique<Monster>();
monster->setPosition(150.0f, 250.0f);
monster->setType(MonsterType::Goblin);
monster->setAggressive(true);
```

### Item System
```cpp
// Create item
auto sword = std::make_unique<Weapon>();
sword->setItemId(1001);
sword->setAttack(15);
sword->setDurability(100);

// Use item
if (player->canUseItem(sword.get())) {
    player->useItem(sword.get());
}
```

### Skill System
```cpp
// Create skill
auto fireball = std::make_unique<MagicSkill>();
fireball->setSkillId(2001);
fireball->setDamage(50);
fireball->setRange(10);
fireball->setCooldown(5.0f);

// Use skill
if (player->canUseSkill(fireball.get())) {
    auto target = findNearestEnemy(player->getPosition());
    player->useSkill(fireball.get(), target);
}
```

## 🔧 System Requirements

### Compiler Requirements
- **C++17** or higher support
- **MariaDB** client library
- **3rdparty/mariadb/**: MariaDB library

### Dependencies
- Engine project (GridManager)
- GameData project (data loading)
- BehaviorTree project (AI)

## 📈 Performance Optimization

### Memory Management
- **Smart Pointers**: Automatic memory management
- **Object Pooling**: Reuse frequently created objects
- **Cache-friendly**: Optimized data structures

### Processing Optimization
- **Batch Processing**: Simultaneous processing of multiple entities
- **Spatial Partitioning**: Efficient spatial queries through GridManager
- **Event System**: Efficient event processing

## 📁 Project Structure

```
Game/
├── Game.vcxproj             # Visual Studio project file
├── Actor.cpp                # Actor implementation
├── Actor.h                  # Actor header
├── BaseItem.h               # Item base class
├── GameData/                # Game data
│   ├── item.bytes           # Item binary data
│   └── skill.bytes          # Skill binary data
├── SQL/                     # Database
│   └── generated/           # Generated SQL files
├── 3rdparty/                # External libraries
│   └── mariadb/             # MariaDB library
├── logs/                    # Game logs
└── x64/                     # Build output
    ├── Debug/
    └── Release/
```

## 🔗 Related Projects

- **[Engine/](../Engine/README.md)** - Game engine (GridManager)
- **[GameData/](../GameData/README.md)** - Game data (JSON)
- **[GameDataFlow/](../GameDataFlow/README.md)** - Data pipeline
- **[BehaviorTree/](../BehaviorTree/README.md)** - AI behavior tree
- **[Battle/](../Battle/README.md)** - Battle server (uses game logic) 
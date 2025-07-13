# Game Data Protobuf

게임 데이터를 위한 Protocol Buffers 정의입니다. 게임 데이터의 구조를 정의하고 바이너리 직렬화를 제공합니다.

## 🎯 주요 기능

### 데이터 구조 정의
- **게임 데이터 스키마**: 아이템, 스킬, 몬스터 등의 데이터 구조
- **바이너리 직렬화**: 효율적인 데이터 직렬화/역직렬화
- **버전 관리**: 스키마 버전 관리 및 호환성

### 코드 생성
- **C++ 코드**: C++ 클라이언트/서버용 코드 생성
- **C# 코드**: .NET 서버용 코드 생성
- **다중 언어**: 다양한 언어에서 사용 가능

## 📁 프로젝트 구조

```
GameDataProtobuf/
├── GameDataProtobuf.vcxproj  # Visual Studio 프로젝트 파일
├── framework.h                # 프레임워크 헤더
├── gamedata.pb.cc            # 생성된 C++ 소스
├── gamedata.pb.h             # 생성된 C++ 헤더
├── gamedata.proto            # Protobuf 정의 파일
└── x64/                      # 빌드 출력
    ├── Debug/
    └── Release/
```

## 📊 Protobuf 정의 예시

### 게임 데이터 스키마
```protobuf
syntax = "proto3";

package gamedata;

// 아이템 데이터
message Item {
    int32 id = 1;
    string name = 2;
    ItemType type = 3;
    int32 level = 4;
    map<string, int32> stats = 5;
}

enum ItemType {
    WEAPON = 0;
    ARMOR = 1;
    CONSUMABLE = 2;
    MATERIAL = 3;
}

// 스킬 데이터
message Skill {
    int32 id = 1;
    string name = 2;
    SkillType type = 3;
    int32 damage = 4;
    float cooldown = 5;
    float range = 6;
    int32 mana_cost = 7;
}

enum SkillType {
    ATTACK = 0;
    BUFF = 1;
    DEBUFF = 2;
    HEAL = 3;
    MOVEMENT = 4;
}

// 몬스터 데이터
message Monster {
    int32 id = 1;
    string name = 2;
    int32 level = 3;
    int32 hp = 4;
    int32 attack = 5;
    int32 defense = 6;
    repeated int32 skill_ids = 7;
}
```

## 🚀 사용 방법

### 1. Protobuf 정의
```protobuf
// gamedata.proto
message Player {
    int32 id = 1;
    string name = 2;
    int32 level = 3;
    int32 experience = 4;
    repeated Item inventory = 5;
}
```

### 2. 코드 생성
```bash
# C++ 코드 생성
protoc --cpp_out=. gamedata.proto

# C# 코드 생성
protoc --csharp_out=. gamedata.proto
```

### 3. 사용 예시
```cpp
// C++ 사용 예시
#include "gamedata.pb.h"

// 데이터 생성
gamedata::Player player;
player.set_id(1);
player.set_name("Player1");
player.set_level(10);

// 직렬화
std::string serialized;
player.SerializeToString(&serialized);

// 역직렬화
gamedata::Player loaded_player;
loaded_player.ParseFromString(serialized);
```

## 🔧 개발 환경

### 요구사항
- Protocol Buffers 컴파일러
- C++17 이상
- CMake 빌드 시스템

### 의존성
- protobuf 라이브러리
- 표준 C++ 라이브러리

## 📈 성능 최적화

### 직렬화 최적화
- **바이너리 형식**: 효율적인 바이너리 직렬화
- **압축**: 데이터 압축을 통한 크기 최적화
- **버전 관리**: 스키마 호환성 유지

### 메모리 관리
- **풀링**: 자주 사용되는 객체 재사용
- **지연 로딩**: 필요할 때만 데이터 로드
- **캐시**: 자주 사용되는 데이터 캐시

## 🔗 관련 프로젝트

- **[GameData/](../GameData/README.md)** - JSON 기반 게임 데이터
- **[GameDataFlow/](../GameDataFlow/README.md)** - 데이터 파이프라인
- **[Game/](../Game/README.md)** - 게임 로직 (생성된 코드 사용)
- **[Battle/](../Battle/README.md)** - 배틀 서버 (게임 데이터 사용) 
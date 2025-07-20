# Game Logic

게임의 핵심 로직을 담당하는 C++ 프로젝트입니다. Actor 시스템, 아이템 시스템, 스킬 시스템 등을 포함합니다.

## 🌍 언어 선택

- [English](README.md) (기본)
- [한국어](README.ko.md)

## 🎯 주요 기능

### Actor 시스템
- **Actor.h**: 모든 게임 엔티티의 기본 클래스
- **캐릭터 관리**: 플레이어, NPC, 몬스터 등
- **상태 관리**: HP, MP, 레벨, 경험치 등
- **위치 관리**: 월드 좌표 및 이동

### 아이템 시스템
- **BaseItem.h**: 아이템 기본 클래스
- **아이템 타입**: 무기, 방어구, 소비아이템 등
- **아이템 효과**: 스탯 증가, 버프, 디버프 등
- **인벤토리 관리**: 아이템 보관 및 관리

### 스킬 시스템
- **스킬 정의**: 스킬 효과, 쿨다운, 범위 등
- **스킬 실행**: 스킬 사용 및 효과 적용
- **스킬 타입**: 공격, 버프, 디버프, 이동 등
- **스킬 레벨**: 스킬 강화 및 레벨업

### 데이터 관리
- **GameData/**: 바이너리 게임 데이터
- **SQL/**: 데이터베이스 스키마 및 쿼리
- **로그 시스템**: 게임 이벤트 로깅

## 🏗️ 아키텍처

### 핵심 클래스 구조

```cpp
class Actor {
    // 모든 게임 엔티티의 기본 클래스
    // - 위치, 상태, 속성 관리
    // - 이벤트 처리
};

class BaseItem {
    // 아이템 기본 클래스
    // - 아이템 속성, 효과 관리
    // - 사용, 장착, 해제
};

class Skill {
    // 스킬 기본 클래스
    // - 스킬 효과, 쿨다운 관리
    // - 타겟팅, 범위 처리
};
```

### 데이터 구조
```
Game/
├── Actor.cpp                # Actor 구현
├── Actor.h                  # Actor 헤더
├── BaseItem.h               # 아이템 기본 클래스
├── GameData/                # 게임 데이터
│   ├── item.bytes           # 아이템 바이너리 데이터
│   └── skill.bytes          # 스킬 바이너리 데이터
├── SQL/                     # 데이터베이스
│   └── generated/           # 생성된 SQL 파일들
└── logs/                    # 게임 로그
```

## 📊 사용 예시

### Actor 생성 및 관리
```cpp
// 플레이어 생성
auto player = std::make_unique<Player>();
player->setPosition(100.0f, 200.0f);
player->setLevel(10);
player->setHP(100);

// 몬스터 생성
auto monster = std::make_unique<Monster>();
monster->setPosition(150.0f, 250.0f);
monster->setType(MonsterType::Goblin);
monster->setAggressive(true);
```

### 아이템 시스템
```cpp
// 아이템 생성
auto sword = std::make_unique<Weapon>();
sword->setItemId(1001);
sword->setAttack(15);
sword->setDurability(100);

// 아이템 사용
if (player->canUseItem(sword.get())) {
    player->useItem(sword.get());
}
```

### 스킬 시스템
```cpp
// 스킬 생성
auto fireball = std::make_unique<MagicSkill>();
fireball->setSkillId(2001);
fireball->setDamage(50);
fireball->setRange(10);
fireball->setCooldown(5.0f);

// 스킬 사용
if (player->canUseSkill(fireball.get())) {
    auto target = findNearestEnemy(player->getPosition());
    player->useSkill(fireball.get(), target);
}
```

## 🔧 시스템 요구사항

### 컴파일러 요구사항
- **C++17** 이상 지원
- **MariaDB** 클라이언트 라이브러리
- **3rdparty/mariadb/**: MariaDB 라이브러리

### 의존성
- Engine 프로젝트 (GridManager)
- GameData 프로젝트 (데이터 로딩)
- BehaviorTree 프로젝트 (AI)

## 📈 성능 최적화

### 메모리 관리
- **스마트 포인터**: 자동 메모리 관리
- **객체 풀링**: 자주 생성되는 객체 재사용
- **캐시 친화적**: 데이터 구조 최적화

### 처리 최적화
- **배치 처리**: 다중 엔티티 동시 처리
- **공간 분할**: GridManager를 통한 효율적인 공간 쿼리
- **이벤트 시스템**: 효율적인 이벤트 처리

## 📁 프로젝트 구조

```
Game/
├── Game.vcxproj             # Visual Studio 프로젝트 파일
├── Actor.cpp                # Actor 구현
├── Actor.h                  # Actor 헤더
├── BaseItem.h               # 아이템 기본 클래스
├── GameData/                # 게임 데이터
│   ├── item.bytes           # 아이템 바이너리 데이터
│   └── skill.bytes          # 스킬 바이너리 데이터
├── SQL/                     # 데이터베이스
│   └── generated/           # 생성된 SQL 파일들
├── 3rdparty/                # 외부 라이브러리
│   └── mariadb/             # MariaDB 라이브러리
├── logs/                    # 게임 로그
└── x64/                     # 빌드 출력
    ├── Debug/
    └── Release/
```

## 🔗 관련 프로젝트

- **[Engine/](../Engine/README.md)** - 게임 엔진 (GridManager)
- **[GameData/](../GameData/README.md)** - 게임 데이터 (JSON)
- **[GameDataFlow/](../GameDataFlow/README.md)** - 데이터 파이프라인
- **[BehaviorTree/](../BehaviorTree/README.md)** - AI 행동 트리
- **[Battle/](../Battle/README.md)** - 배틀 서버 (게임 로직 사용) 
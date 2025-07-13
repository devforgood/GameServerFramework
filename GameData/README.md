# Game Data

게임 데이터를 JSON 기반으로 관리하는 프로젝트입니다. 스킬, 아이템, 퀘스트 등의 게임 데이터를 정의하고 관리합니다.

## 🎯 주요 기능

### 데이터 타입
- **스킬 데이터**: 스킬 효과, 쿨다운, 범위 등
- **아이템 데이터**: 아이템 속성, 등급, 효과 등
- **퀘스트 데이터**: 퀘스트 목표, 보상, 조건 등
- **몬스터 데이터**: 몬스터 속성, AI 패턴 등

### 다국어 지원
- **localization_en.json**: 영어 로컬라이제이션
- **localization_ko.json**: 한국어 로컬라이제이션

## 📁 프로젝트 구조

```
GameData/
├── GameData.csproj           # 프로젝트 파일
├── item.json                 # 아이템 데이터
├── skill.json                # 스킬 데이터
├── quest.json                # 퀘스트 데이터
├── Localization/             # 다국어 지원
│   ├── localization_en.json  # 영어
│   └── localization_ko.json  # 한국어
└── obj/                      # 빌드 출력
```

## 📊 데이터 형식

### 아이템 데이터 예시
```json
{
  "items": [
    {
      "id": 1001,
      "name": "Iron Sword",
      "type": "Weapon",
      "rarity": "Common",
      "stats": {
        "attack": 15,
        "durability": 100
      },
      "description": "A basic iron sword"
    }
  ]
}
```

### 스킬 데이터 예시
```json
{
  "skills": [
    {
      "id": 2001,
      "name": "Fireball",
      "type": "Magic",
      "damage": 50,
      "range": 10,
      "cooldown": 5.0,
      "mana_cost": 20,
      "description": "Launches a fireball at the target"
    }
  ]
}
```

### 퀘스트 데이터 예시
```json
{
  "quests": [
    {
      "id": 3001,
      "name": "First Steps",
      "type": "Main",
      "objectives": [
        {
          "type": "Kill",
          "target": "Goblin",
          "count": 5
        }
      ],
      "rewards": {
        "exp": 100,
        "gold": 50,
        "items": [1001]
      }
    }
  ]
}
```

## 🔧 개발 환경

### 요구사항
- .NET 6.0 이상
- JSON 파싱 라이브러리

### 의존성
- JSON.NET (Newtonsoft.Json)
- System.Text.Json

## 🔗 관련 프로젝트

- **[GameDataFlow/](../GameDataFlow/README.md)** - 데이터 파이프라인 (JSON → Protobuf)
- **[GameDataProtobuf/](../GameDataProtobuf/README.md)** - Protobuf 정의
- **[Game/](../Game/README.md)** - 게임 로직 (데이터 사용)
- **[Battle/](../Battle/README.md)** - 배틀 서버 (스킬, 아이템 데이터) 
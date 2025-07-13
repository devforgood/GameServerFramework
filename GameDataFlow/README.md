# Game Data Flow

MMORPG 데이터 기반 설계 및 자동 코드 생성 파이프라인입니다. JSON → Protobuf 변환 → 코드 자동 생성 → 런타임에서 ID 기반 팩토리 생성으로 이어지는 구조를 제공합니다.

## 🎯 주요 기능

### 데이터 파이프라인
- **JSON → Protobuf 변환**: 사람이 읽기 쉬운 JSON을 빠른 바이너리 형식으로 변환
- **자동 코드 생성**: Factory 패턴 기반 C++ 코드 자동 생성
- **ID 기반 팩토리**: `SkillFactory::Create(id)` 형태로 객체 생성

### 재사용 가능한 속성 구조
- **Property 기반 설계**: `hp`, `attack`, `defense` 등 공통 속성 분리
- **컴포넌트 시스템**: Component-based Design과 궁합이 좋음
- **참조 방식**: 공통 속성을 별도 테이블로 분리하고 참조

## 🏗️ 아키텍처

### 전체 데이터 파이프라인 개요

1. **디자이너가 JSON으로 게임 데이터를 작성**
   - 사람이 읽고 편집하기 쉬운 형태
   - 예: 스킬, 아이템, 퀘스트, 몬스터 등

2. **JSON → Protobuf 변환 및 바이너리 시리얼라이징**
   - `.proto` 파일 기반 정의
   - 빠른 로딩과 코드 자동 생성을 위해 protobuf 사용

3. **자동 코드 생성 (Factory 패턴 기반)**
   - 각 ID에 해당하는 클래스를 자동으로 매핑하는 C++ 팩토리 코드 생성
   - `SkillFactory::Create(id)` 형태로 객체 생성

## 📊 자동 생성된 팩토리 코드 예시

```cpp
#include "Common.h" 
#include "SkillFactory.h"

#include "JumpSkill.h"
#include "NormalAttackSkill.h"

Skill* SkillFactory::Create(int32_t id) {
    Skill* obj = nullptr;
    switch (id) {
        case 2: obj = new JumpSkill(); break;
        case 1: obj = new NormalAttackSkill(); break;
        default: return nullptr;
    }

    obj->gamedata = ResourceLoader::Instance().GetSkills(id);
    return obj;
}
```

- `Create()` 함수는 **스킬 ID를 기반으로 해당 스킬 객체를 생성**
- `gamedata`는 ID에 해당하는 시리얼라이즈된 데이터 (`protobuf`)를 로드
- 새로운 스킬 추가 시, `.proto` + JSON + 정의만 하면 이 코드가 자동으로 갱신됨

## 💡 사용 예시

```cpp
for (const auto& skill : skills) {
    skills_[skill.first] = SkillFactory::Create(skill.first);
}
```

- `skills`는 로드된 데이터의 ID 집합
- 런타임에서 ID를 기준으로 Skill 인스턴스를 생성하여 `skills_` 맵에 저장

## 📦 Protobuf의 장점

- **이진 포맷**: 메모리 효율 + 빠른 로딩
- **유연한 스키마 변경**: 필드 추가/삭제에 강함
- **코드 자동 생성**: 서버/클라이언트 모두 동일 구조 사용 가능
- **멀티 플랫폼 대응**: C++, C#, Python 등에서 활용 가능

## 🔁 재사용 가능한 속성 구조

예: `hp`, `attack`, `defense` 등의 속성은 공통으로 여러 엔티티에서 사용
공통 속성을 별도 테이블/구조로 분리하고 참조 방식으로 설계

```json
{
  "property_id": 2001,
  "hp": 300,
  "attack": 45
}
```

## 📁 프로젝트 구조

```
GameDataFlow/
├── build.bat               # 빌드 스크립트
├── gamedata.proto          # Protobuf 정의
├── gamedata_pb2.py         # Python Protobuf 생성 파일
├── templates/              # 코드 생성 템플릿
│   ├── Base.h.j2           # C++ 헤더 템플릿
│   ├── Factory.cpp.j2      # C++ 팩토리 구현 템플릿
│   ├── Factory.cs.j2       # C# 팩토리 템플릿
│   └── ResourceLoader.j2   # 리소스 로더 템플릿
└── generated/              # 생성된 파일들
    ├── SkillFactory.cpp    # 자동 생성된 스킬 팩토리
    ├── ItemFactory.cpp     # 자동 생성된 아이템 팩토리
    └── ResourceLoader.cpp  # 자동 생성된 리소스 로더
```

## 🔧 개발 환경

### 요구사항
- Python 3.7 이상
- Protobuf 컴파일러
- Jinja2 템플릿 엔진

### 의존성
- protobuf
- jinja2
- click (CLI 도구)

## 🚀 사용 방법

### 1. 데이터 정의
```bash
# JSON 데이터 작성
vim GameData/item.json
vim GameData/skill.json
```

### 2. Protobuf 정의
```bash
# .proto 파일 수정
vim gamedata.proto
```

### 3. 코드 생성
```bash
# 자동 코드 생성
python generate_code.py --input GameData/ --output generated/
```

### 4. 빌드
```bash
# C++ 코드 빌드
./build.bat
```

## ✅ 핵심 요약

| 항목            | 설명                                    |
| ------------- | ------------------------------------- |
| JSON          | 사람이 작성, 직관적 버전 관리                     |
| Protobuf      | 빠른 로딩, 코드 자동 생성, 버전 안정성               |
| Factory 자동 생성 | ID 기반 객체 생성 코드 자동화                    |
| 재사용 구조        | 공통 속성(Property) 및 컴포넌트 기반 설계          |
| 런타임 사용        | `SkillFactory::Create(id)` 형태로 코드 단순화 |

## 🔗 관련 프로젝트

- **[GameData/](../GameData/README.md)** - JSON 기반 게임 데이터
- **[GameDataProtobuf/](../GameDataProtobuf/README.md)** - Protobuf 정의
- **[Game/](../Game/README.md)** - 게임 로직 (생성된 팩토리 사용)
- **[Battle/](../Battle/README.md)** - 배틀 서버 (스킬 팩토리 사용) 
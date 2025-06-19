## Introduction

게임 서버 프레임워크는 오픈소스를 활용하여 게임 서버 개발 기간을 단축하기 위해 만들어졌다. MMORPG, MORPG, PVP, 액션등의 장르등을 지원할 계획이다. 주사용 언어는 C#, C++로 이루어졌다.

## Requirements

* [gRPC](https://github.com/grpc/grpc)
* [flatbuffers](https://github.com/google/flatbuffers)
* [recastnavigation](https://github.com/recastnavigation/recastnavigation)
* [lidgren](https://github.com/lidgren/lidgren-network-gen3)
* [Hazel-Networking](https://github.com/DarkRiftNetworking/Hazel-Networking)
* [BEPUPhysics](https://github.com/bepu/bepuphysics1)
* [BehaviorTree.CPP](https://github.com/BehaviorTree/BehaviorTree.CPP)

## Prepare
```bash
./vcpkg install behaviortree-cpp
./vcpkg install protobuf:x64-windows
```


## 🛠️ MMORPG 데이터 기반 설계 및 자동 코드 생성 파이프라인

이 프로젝트는 MMORPG 게임에 필요한 다양한 게임 데이터를 **유연하고 재사용 가능하며 유지보수가 쉬운 방식으로 설계하고 자동화**하는 것을 목표로 합니다.

데이터 파이프라인은 JSON → Protobuf 변환 → 코드 자동 생성 → 런타임에서 ID 기반 팩토리 생성으로 이어집니다. 이 구조는 복잡한 게임 로직에서도 **일관된 데이터 처리**, **타입 안정성**, **유연한 확장성**을 제공합니다.

---

### 📈 전체 데이터 파이프라인 개요

1. **디자이너가 JSON으로 게임 데이터를 작성**

   * 사람이 읽고 편집하기 쉬운 형태
   * 예: 스킬, 아이템, 퀘스트, 몬스터 등

2. **JSON → Protobuf 변환 및 바이너리 시리얼라이징**

   * `.proto` 파일 기반 정의
   * 빠른 로딩과 코드 자동 생성을 위해 protobuf 사용

3. **자동 코드 생성 (Factory 패턴 기반)**

   * 각 ID에 해당하는 클래스를 자동으로 매핑하는 C++ 팩토리 코드 생성
   * `SkillFactory::Create(id)` 형태로 객체 생성

---

### ⚙️ 자동 생성된 팩토리 코드 예시

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

* `Create()` 함수는 **스킬 ID를 기반으로 해당 스킬 객체를 생성**
* `gamedata`는 ID에 해당하는 시리얼라이즈된 데이터 (`protobuf`)를 로드
* 새로운 스킬 추가 시, `.proto` + JSON + 정의만 하면 이 코드가 자동으로 갱신됨

---

### 💡 사용 예시

```cpp
for (const auto& skill : skills) {
    skills_[skill.first] = SkillFactory::Create(skill.first);
}
```

* `skills`는 로드된 데이터의 ID 집합
* 런타임에서 ID를 기준으로 Skill 인스턴스를 생성하여 `skills_` 맵에 저장

---

### 📦 Protobuf의 장점

* **이진 포맷**: 메모리 효율 + 빠른 로딩
* **유연한 스키마 변경**: 필드 추가/삭제에 강함
* **코드 자동 생성**: 서버/클라이언트 모두 동일 구조 사용 가능
* **멀티 플랫폼 대응**: C++, C#, Python 등에서 활용 가능

---

### 🔁 재사용 가능한 속성 구조 (Property 기반 설계)

* 예: `hp`, `attack`, `defense` 등의 속성은 공통으로 여러 엔티티에서 사용
* 공통 속성을 별도 테이블/구조로 분리하고 참조 방식으로 설계
* 컴포넌트 시스템(Component-based Design)과 궁합이 좋음

```json
{
  "property_id": 2001,
  "hp": 300,
  "attack": 45
}
```

---

### ✅ 핵심 요약

| 항목            | 설명                                    |
| ------------- | ------------------------------------- |
| JSON          | 사람이 작성, 직관적 버전 관리                     |
| Protobuf      | 빠른 로딩, 코드 자동 생성, 버전 안정성               |
| Factory 자동 생성 | ID 기반 객체 생성 코드 자동화                    |
| 재사용 구조        | 공통 속성(Property) 및 컴포넌트 기반 설계          |
| 런타임 사용        | `SkillFactory::Create(id)` 형태로 코드 단순화 |

---

이 시스템은 MMORPG의 복잡한 데이터 구조를 **정형화된 파이프라인과 코드 자동화**로 효율적으로 관리할 수 있게 해줍니다. 디자이너와 개발자 간 협업은 쉬워지고, 유지보수성과 확장성은 극대화됩니다.



## Server Architecture
![severArchitecture](https://user-images.githubusercontent.com/17477292/115057890-8e971280-9f1f-11eb-8043-6dbc64521900.png)

# 서버 구성
1) 로비서버
* 유저 인증, 매칭등 기타 인게임을 제외한 모든 기능을 담당한다.
* 주기적 매칭 시도 방식 :
 클라이언트가 풀링하듯이 일정 시간마다 요청 매칭 조건이 만족하면 매칭 성공
* 매칭 등록후 대기 방식 : 
 클라이언트가 매칭 등록을 해놓고, redis sub을 통해 대기, grpc message stream으로 대기
 다음 클라이언트가 매칭 시도에서 성공시 redis pub으로 대기중 유저에 알림

2) 배틀서버
* 인게임 캐릭터 이동 및 스킬 동기화를 담당한다

3) AI 서버
* AI 상태 관리, 길찾기를 담당한다.
* 에이전트 생성, 삭제, 목표지점 이동

# 인게임 동기화
1) 캐릭터 이동 동기화
* 클라이언트에서 서버로 키입력 동기화, 일정 주기 마다 샘플링된 키값을 큐잉하여 서버로 송신 이때 큐에 있는 키값은 과거 데이터를 포함한 리스트 형태로 보내어 패킷 유실이 발생하여도 문제가 없도록한다.
* 키입력을 수신 받은 서버는 이동 시뮬레이션 후 현재 좌표, 타임스탬프를 클라이언트에 송신, 클라이언트는 서버로 부터 받은 타임스탬프, 좌표로 수정하고 큐에 저장된 키입력 중 타임스탬프 이후 키입력 반영 (이동  되감기)
2) 상태 동기화
* 게임 오브젝트 상태 동기화 : 일정 주기 마다 월드에서 상태가 변경된 오브젝트만 동기화 (리플리케이션 - 변수 복제)
3) 스킬 동기화
* 각 스킬 고유한 정보를 RPC 호출로 동기화


# 프로토콜
1) gRPC
* 모바일 환경(빈번한 접속 종료)에서는 stateless protocol이 적합
* Server side Push
* Load balance 가 용이
* Google 신뢰도

2) RUDP
* TCP의 불필요한 처리(흐름제어, 혼잡제어)가 없어 latency 확보 용이
* Multiplexing : 한 소켓을 용도별 요청 가능 (채널)
* QoS : 패킷 유실 보장


## Sequence Diagram
### 1. Login sequence diagram
![login](https://user-images.githubusercontent.com/17477292/115049395-a4073f00-9f15-11eb-9a40-04d1922dec97.png)

### 1. Game result sequence diagram
![gameresult](https://user-images.githubusercontent.com/17477292/115050008-4a534480-9f16-11eb-83b3-864546550313.png)

### 1. Match sequence diagram
![match](https://user-images.githubusercontent.com/17477292/115050031-50492580-9f16-11eb-80f7-c55eae32d863.png)

### 1. Match version 2 diagram
![match2](https://user-images.githubusercontent.com/17477292/115050025-4e7f6200-9f16-11eb-958f-7e459fa23cc7.png)


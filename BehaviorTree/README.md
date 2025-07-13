# Behavior Tree

AI 행동 트리 시스템입니다. 복잡한 AI 로직을 트리 구조로 표현하여 유지보수성과 확장성을 제공합니다.

## 🎯 주요 기능

### 행동 트리 노드 타입
- **Action Node**: 실제 행동을 수행하는 노드
- **Condition Node**: 조건을 확인하는 노드
- **Composite Node**: 여러 자식 노드를 관리하는 노드
  - **Sequence**: 모든 자식이 성공해야 성공
  - **Selector**: 하나라도 성공하면 성공
  - **Parallel**: 모든 자식을 동시에 실행

### AI 패턴
- **순차 실행**: 특정 순서대로 행동 수행
- **조건부 실행**: 조건에 따라 다른 행동 선택
- **병렬 실행**: 여러 행동을 동시에 수행
- **우선순위 기반**: 우선순위에 따라 행동 선택

## 🏗️ 아키텍처

### 핵심 클래스 구조

```cpp
class BehaviorTree {
    // 메인 행동 트리 클래스
    // - 트리 구조 관리
    // - 노드 실행 및 결과 처리
};

class BehaviorNode {
    // 모든 노드의 기본 클래스
    // - 실행 상태 관리
    // - 자식 노드 관리
};

class ActionNode : public BehaviorNode {
    // 실제 행동을 수행하는 노드
    // - 이동, 공격, 아이템 사용 등
};

class ConditionNode : public BehaviorNode {
    // 조건을 확인하는 노드
    // - HP 체크, 거리 체크, 상태 체크 등
};
```

### 노드 실행 상태
- **SUCCESS**: 노드가 성공적으로 완료
- **FAILURE**: 노드가 실패
- **RUNNING**: 노드가 실행 중
- **IDLE**: 노드가 대기 중

## 📊 사용 예시

### 기본 행동 트리 구성
```cpp
// AI 몬스터의 기본 행동 트리
auto root = std::make_unique<SelectorNode>();

// 1. 공격 가능한지 확인하고 공격
auto attackSequence = std::make_unique<SequenceNode>();
attackSequence->addChild(std::make_unique<CanAttackCondition>());
attackSequence->addChild(std::make_unique<AttackAction>());
root->addChild(std::move(attackSequence));

// 2. 플레이어가 시야에 있는지 확인하고 추적
auto chaseSequence = std::make_unique<SequenceNode>();
chaseSequence->addChild(std::make_unique<PlayerInSightCondition>());
chaseSequence->addChild(std::make_unique<ChaseAction>());
root->addChild(std::move(chaseSequence));

// 3. 기본 행동 (순찰)
auto patrolSequence = std::make_unique<SequenceNode>();
patrolSequence->addChild(std::make_unique<PatrolAction>());
root->addChild(std::move(patrolSequence));
```

### 조건 노드 예시
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

### 액션 노드 예시
```cpp
class AttackAction : public ActionNode {
public:
    Status execute() override {
        auto target = getTarget();
        if (!target) return Status::FAILURE;
        
        // 공격 로직 수행
        performAttack(target);
        return Status::SUCCESS;
    }
};
```

## 🔧 시스템 요구사항

### 컴파일러 요구사항
- **C++17** 이상 지원
- **STL** 컨테이너 사용

### 헤더 파일
```cpp
#include <memory>       // 스마트 포인터
#include <vector>       // 동적 배열
#include <functional>   // 함수 객체
```

## 📈 성능 최적화

### 메모리 관리
- **스마트 포인터**: 자동 메모리 관리
- **노드 풀링**: 자주 사용되는 노드 재사용
- **캐시 친화적**: 노드 데이터 구조 최적화

### 실행 최적화
- **조기 종료**: 조건 실패 시 즉시 종료
- **병렬 실행**: 독립적인 노드 동시 실행
- **우선순위 기반**: 중요한 행동 우선 실행

## 📁 프로젝트 구조

```
BehaviorTree/
├── BehaviorTree.vcxproj     # Visual Studio 프로젝트 파일
├── BehaviorTree.vcxproj.filters # 프로젝트 필터
├── Behavior.h               # 행동 트리 헤더
├── Behavior.cpp             # 행동 트리 구현
├── BehaviorEvent.h          # 행동 이벤트 헤더
├── BehaviorEvent.cpp        # 행동 이벤트 구현
├── Nodes/                   # 노드 클래스들
│   ├── ActionNode.h         # 액션 노드
│   ├── ConditionNode.h      # 조건 노드
│   ├── CompositeNode.h      # 복합 노드
│   └── DecoratorNode.h      # 데코레이터 노드
└── x64/                     # 빌드 출력
    └── Debug/
```

## 🎮 AI 패턴 예시

### 몬스터 AI
```cpp
// 몬스터 행동 트리
auto monsterAI = std::make_unique<SelectorNode>();

// 1. HP가 낮으면 도망
auto fleeSequence = std::make_unique<SequenceNode>();
fleeSequence->addChild(std::make_unique<LowHPCondition>());
fleeSequence->addChild(std::make_unique<FleeAction>());
monsterAI->addChild(std::move(fleeSequence));

// 2. 공격 가능하면 공격
auto attackSequence = std::make_unique<SequenceNode>();
attackSequence->addChild(std::make_unique<CanAttackCondition>());
attackSequence->addChild(std::make_unique<AttackAction>());
monsterAI->addChild(std::move(attackSequence));

// 3. 플레이어 추적
auto chaseSequence = std::make_unique<SequenceNode>();
chaseSequence->addChild(std::make_unique<PlayerInSightCondition>());
chaseSequence->addChild(std::make_unique<ChaseAction>());
monsterAI->addChild(std::move(chaseSequence));

// 4. 순찰
auto patrolSequence = std::make_unique<SequenceNode>();
patrolSequence->addChild(std::make_unique<PatrolAction>());
monsterAI->addChild(std::move(patrolSequence));
```

### NPC AI
```cpp
// NPC 행동 트리
auto npcAI = std::make_unique<ParallelNode>();

// 1. 대화 가능한 플레이어 확인 (병렬 실행)
npcAI->addChild(std::make_unique<CheckConversationAction>());

// 2. 기본 애니메이션 재생 (병렬 실행)
npcAI->addChild(std::make_unique<PlayIdleAnimationAction>());

// 3. 주변 환경 반응 (병렬 실행)
npcAI->addChild(std::make_unique<ReactToEnvironmentAction>());
```

## 🔗 관련 프로젝트

- **[Game/](../Game/README.md)** - 게임 로직 (AI 엔티티)
- **[Engine/](../Engine/README.md)** - 게임 엔진 (공간 분할)
- **[Battle/](../Battle/README.md)** - 배틀 서버 (AI 동기화)
- **[FiniteStateMachine/](../FiniteStateMachine/README.md)** - 유한 상태 머신 
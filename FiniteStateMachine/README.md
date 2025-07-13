# Finite State Machine

유한 상태 머신 시스템입니다. 게임 엔티티의 상태 변화를 관리하고 상태 기반 행동을 구현합니다.

## 🎯 주요 기능

### 상태 관리
- **상태 정의**: 각 엔티티의 가능한 상태 정의
- **상태 전환**: 조건에 따른 상태 변화
- **상태 기반 행동**: 각 상태에서의 행동 정의

### 엔티티 관리
- **BaseGameEntity**: 모든 게임 엔티티의 기본 클래스
- **상태 업데이트**: 주기적인 상태 업데이트
- **이벤트 처리**: 상태 변화 이벤트 처리

## 🏗️ 아키텍처

### 핵심 클래스 구조

```cpp
class BaseGameEntity {
    // 모든 게임 엔티티의 기본 클래스
    // - 상태 관리
    // - 위치 및 속성 관리
};

class State {
    // 상태 기본 클래스
    // - 상태 진입/종료 처리
    // - 상태별 행동 정의
};

class StateMachine {
    // 상태 머신 관리자
    // - 현재 상태 관리
    // - 상태 전환 처리
};
```

## 📊 사용 예시

### 상태 정의
```cpp
class IdleState : public State {
public:
    void enter(BaseGameEntity* entity) override {
        // 대기 상태 진입 처리
    }
    
    void execute(BaseGameEntity* entity) override {
        // 대기 상태 행동
        // 주변 탐지, 애니메이션 재생 등
    }
    
    void exit(BaseGameEntity* entity) override {
        // 대기 상태 종료 처리
    }
};
```

### 상태 머신 사용
```cpp
class Monster : public BaseGameEntity {
private:
    StateMachine stateMachine;
    
public:
    Monster() {
        // 상태 머신 초기화
        stateMachine.addState(new IdleState());
        stateMachine.addState(new ChaseState());
        stateMachine.addState(new AttackState());
        
        // 초기 상태 설정
        stateMachine.changeState(StateType::IDLE);
    }
    
    void update() override {
        // 상태 머신 업데이트
        stateMachine.update();
    }
};
```

## 📁 프로젝트 구조

```
FiniteStateMachine/
├── FiniteStateMachine.vcxproj     # Visual Studio 프로젝트 파일
├── FiniteStateMachine.vcxproj.filters # 프로젝트 필터
├── BaseGameEntity.cpp             # 기본 게임 엔티티 구현
├── BaseGameEntity.h               # 기본 게임 엔티티 헤더
├── State.h                        # 상태 기본 클래스
├── StateMachine.h                 # 상태 머신 헤더
└── x64/                           # 빌드 출력
    └── Debug/
```

## 🔧 시스템 요구사항

### 컴파일러 요구사항
- **C++17** 이상 지원
- **STL** 컨테이너 사용

### 의존성
- Engine 프로젝트 (공간 분할)
- Game 프로젝트 (게임 로직)

## 🔗 관련 프로젝트

- **[Game/](../Game/README.md)** - 게임 로직 (엔티티 시스템)
- **[Engine/](../Engine/README.md)** - 게임 엔진 (공간 분할)
- **[BehaviorTree/](../BehaviorTree/README.md)** - AI 행동 트리 
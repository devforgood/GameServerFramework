# Game Server Framework

게임 서버 프레임워크는 오픈소스를 활용하여 게임 서버 개발 기간을 단축하기 위해 만들어졌습니다. MMORPG, MORPG, PVP, 액션 등의 장르를 지원할 계획입니다. 주사용 언어는 C#, C++로 구성되어 있습니다.

## 🌍 언어 선택

- [English](README.md) (기본)
- [한국어](README.ko.md)

## 🏗️ 프로젝트 구조

### 📁 서버 프로젝트
- **[Battle/](Battle/README.md)** - 배틀 서버 (인게임 캐릭터 이동 및 스킬 동기화)
- **[Lobby/](Lobby/README.md)** - 로비 서버 (유저 인증, 매칭 등)
- **[Login/](Login/README.md)** - 로그인 서버
- **[Chat/](Chat/README.md)** - 채팅 서버
- **[Cache/](Cache/README.md)** - 캐시 서버
- **[IAP/](IAP/README.md)** - 인앱 결제 서버
- **[GmTool/](GmTool/README.md)** - GM 도구

### 📁 핵심 라이브러리
- **[Engine/](Engine/README.md)** - 게임 엔진 (GridManager, 공간 분할 시스템)
- **[BehaviorTree/](BehaviorTree/README.md)** - AI 행동 트리
- **[FiniteStateMachine/](FiniteStateMachine/README.md)** - 유한 상태 머신
- **[Game/](Game/README.md)** - 게임 로직 (Actor, 아이템, 스킬 시스템)
- **[recastnavigation/](recastnavigation/README.md)** - 길찾기 시스템

### 📁 데이터 및 도구
- **[GameData/](GameData/README.md)** - 게임 데이터 (JSON 기반)
- **[GameDataFlow/](GameDataFlow/README.md)** - 데이터 파이프라인 (JSON → Protobuf)
- **[GameDataProtobuf/](GameDataProtobuf/README.md)** - Protobuf 정의
- **[SqlCodeGenerator/](SqlCodeGenerator/README.md)** - SQL 코드 생성기
- **[Models/](Models/README.md)** - 데이터 모델

### 📁 클라이언트
- **[Client/](Client/README.md)** - Unity 클라이언트 (길찾기 테스트 도구 포함)

### 📁 외부 라이브러리
- **[flatbuffer/](flatbuffer/README.md)** - FlatBuffers 직렬화
- **[protos/](protos/README.md)** - gRPC 프로토콜 정의

## 🚀 빠른 시작

### 요구사항
```bash
# vcpkg를 통한 의존성 설치
./vcpkg install behaviortree-cpp
./vcpkg install protobuf:x64-windows
```

### 주요 의존성
- [gRPC](https://github.com/grpc/grpc) - 통신 프로토콜
- [flatbuffers](https://github.com/google/flatbuffers) - 직렬화
- [recastnavigation](https://github.com/recastnavigation/recastnavigation) - 길찾기
- [lidgren](https://github.com/lidgren/lidgren-network-gen3) - 네트워킹
- [Hazel-Networking](https://github.com/DarkRiftNetworking/Hazel-Networking) - 네트워킹
- [BEPUPhysics](https://github.com/bepu/bepuphysics1) - 물리 엔진
- [BehaviorTree.CPP](https://github.com/BehaviorTree/BehaviorTree.CPP) - AI 행동 트리

## 🏛️ 서버 아키텍처

![Server Architecture](https://user-images.githubusercontent.com/17477292/115057890-8e971280-9f1f-11eb-8043-6dbc64521900.png)

### 서버 구성
1. **로비 서버** - 유저 인증, 매칭 등 인게임을 제외한 모든 기능
2. **배틀 서버** - 인게임 캐릭터 이동 및 스킬 동기화
3. **AI 서버** - AI 상태 관리, 길찾기

### 인게임 동기화
1. **캐릭터 이동 동기화** - 클라이언트 키입력 → 서버 시뮬레이션 → 클라이언트 수정
2. **상태 동기화** - 변경된 오브젝트만 주기적 동기화 (리플리케이션)
3. **스킬 동기화** - RPC 호출로 스킬 정보 동기화

### 프로토콜
- **gRPC** - 모바일 환경에 적합한 stateless protocol
- **RUDP** - TCP의 불필요한 처리 없이 latency 확보

## 📊 시퀀스 다이어그램

### 로그인 시퀀스
![Login Sequence](https://user-images.githubusercontent.com/17477292/115049395-a4073f00-9f15-11eb-9a40-04d1922dec97.png)

### 게임 결과 시퀀스
![Game Result Sequence](https://user-images.githubusercontent.com/17477292/115050008-4a534480-9f16-11eb-9a40-04d1922dec97.png)

### 매칭 시퀀스
![Match Sequence](https://user-images.githubusercontent.com/17477292/115050031-50492580-9f16-11eb-80f7-c55eae32d863.png)

### 매칭 v2 시퀀스
![Match v2 Sequence](https://user-images.githubusercontent.com/17477292/115050025-4e7f6200-9f16-11eb-80f7-c55eae32d863.png)

## 🛠️ 개발 도구

### Unity 클라이언트 도구
- **Sample Terrain Generator** - 길찾기 테스트용 지형 생성
- **Advanced Terrain Generator** - 고급 지형 생성 (다양한 테스트 시나리오)
- **Pathfinding Test Manager** - 길찾기 에이전트 관리

### 데이터 파이프라인
- **JSON → Protobuf 변환** - 빠른 로딩과 코드 자동 생성
- **Factory 패턴 자동 생성** - ID 기반 객체 생성 코드 자동화
- **재사용 가능한 속성 구조** - 컴포넌트 기반 설계

## 📈 성능 최적화

### GridManager
- **SIMD 벡터화** - AVX2 명령어로 8개 거리 동시 계산
- **삼각함수 Look-up 테이블** - 미리 계산된 값으로 빠른 각도 계산
- **적응형 그리드 구조** - 데이터 밀도에 따른 Vector/HashMap 자동 선택

### 처리 성능
- **10,000개 엔티티**: AoE 쿼리 < 1ms
- **100,000개 엔티티**: 시야 범위 검색 < 5ms
- **SIMD 최적화**: 기존 대비 3-8배 성능 향상

## 🤝 기여하기

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 라이선스

이 프로젝트는 MIT 라이선스 하에 배포됩니다. 자세한 내용은 `LICENSE` 파일을 참조하세요.

## 📞 연락처

프로젝트 링크: [https://github.com/yourusername/GameServerFramework](https://github.com/yourusername/GameServerFramework) 
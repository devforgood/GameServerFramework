## Introduction

게임 서버 프레임워크는 오픈소스를 활용하여 게임 서버 개발 기간을 단축하기 위해 만들어졌다. MMORPG, MORPG, PVP, 액션등의 장르등을 지원할 계획이다. 주사용 언어는 C#, C++로 이루어졌다.

## Requirements

* [gRPC](https://github.com/grpc/grpc)
* [flatbuffers](https://github.com/google/flatbuffers)
* [recastnavigation](https://github.com/recastnavigation/recastnavigation)
* [lidgren](https://github.com/lidgren/lidgren-network-gen3)
* [Hazel-Networking](https://github.com/DarkRiftNetworking/Hazel-Networking)
* [BEPUPhysics](https://github.com/bepu/bepuphysics1)

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


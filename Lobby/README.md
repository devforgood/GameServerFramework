# Lobby Server

유저 인증, 매칭 등 인게임을 제외한 모든 기능을 담당하는 로비 서버입니다.

## 🎯 주요 기능

### 유저 인증
- **로그인 처리**: 사용자 인증 및 세션 관리
- **OAuth 지원**: Facebook, GameCenter 등 외부 인증 제공자 지원
- **세션 관리**: 사용자 세션 생성 및 관리

### 매칭 시스템
- **주기적 매칭 시도**: 클라이언트가 풀링하듯이 일정 시간마다 요청, 매칭 조건이 만족하면 매칭 성공
- **매칭 등록 후 대기**: 클라이언트가 매칭 등록을 해놓고, Redis sub을 통해 대기, gRPC message stream으로 대기
- **Redis Pub/Sub**: 다음 클라이언트가 매칭 시도에서 성공시 Redis pub으로 대기중 유저에 알림

### 게임 관리
- **서버 상태 관리**: 배틀 서버 상태 모니터링
- **게임 세션 관리**: 게임 세션 생성 및 관리
- **결과 처리**: 게임 결과 저장 및 통계

## 🏗️ 아키텍처

### 핵심 컴포넌트
- **Authentication Service**: 사용자 인증 처리
- **Matching Service**: 매칭 로직 처리
- **Game Session Manager**: 게임 세션 관리
- **Redis Cache**: 세션 및 매칭 데이터 캐시

### 매칭 플로우
1. **매칭 요청**: 클라이언트가 매칭 조건과 함께 요청
2. **매칭 검색**: 조건에 맞는 다른 플레이어 검색
3. **매칭 성공**: 조건 만족 시 매칭 성공 알림
4. **게임 세션 생성**: 배틀 서버에 게임 세션 생성 요청

## 📊 매칭 방식

### 주기적 매칭 시도
```csharp
// 클라이언트가 주기적으로 매칭 요청
while (!isMatched) {
    var matchResult = await lobbyClient.RequestMatch(matchCriteria);
    if (matchResult.IsSuccess) {
        // 매칭 성공 처리
        break;
    }
    await Task.Delay(1000); // 1초 대기
}
```

### 매칭 등록 후 대기
```csharp
// 매칭 등록
await lobbyClient.RegisterForMatch(matchCriteria);

// Redis sub을 통한 대기
using var subscription = redisClient.Subscribe("match_notifications");
await subscription.OnMessage(async (channel, message) => {
    // 매칭 성공 알림 처리
});
```

## 🚀 사용 예시

### 서버 시작
```bash
cd Lobby
dotnet run
```

### 설정 파일
- `appsettings.json`: 기본 설정
- `appsettings.Release.json`: 배포 환경 설정

## 📁 프로젝트 구조

```
Lobby/
├── Lobby.csproj           # 프로젝트 파일
├── appsettings.json       # 기본 설정
├── appsettings.Release.json # 배포 설정
├── GrpcHostedService.cs   # gRPC 호스팅 서비스
├── Modules/               # 비즈니스 로직 모듈
│   ├── Authentication/    # 인증 관련
│   ├── Matching/          # 매칭 관련
│   └── GameSession/       # 게임 세션 관련
├── OAuth/                 # OAuth 인증
│   ├── ExternalProvider.cs
│   ├── ExternalProviderFacebook.cs
│   └── ExternalProviderGameCenter.cs
├── Service/               # 서비스 레이어
├── Query/                 # 데이터 쿼리
├── WebAPIClient/          # 웹 API 클라이언트
└── UnitTest/              # 단위 테스트
```

## 🔧 개발 환경

### 요구사항
- .NET 6.0 이상
- gRPC
- Redis
- Entity Framework Core

### 의존성
- gRPC 통신
- Redis 캐시
- OAuth 인증 제공자
- Battle 서버와의 통신

## 📈 모니터링

### 로그
- 구조화된 로깅 지원
- 매칭 성공률 추적
- 인증 실패 로그

### 메트릭
- 동시 접속자 수
- 매칭 대기 시간
- 매칭 성공률
- 인증 처리 시간

## 🔗 관련 프로젝트

- **[Battle/](../Battle/README.md)** - 배틀 서버 (게임 세션)
- **[Login/](../Login/README.md)** - 로그인 서버
- **[Cache/](../Cache/README.md)** - 캐시 서버
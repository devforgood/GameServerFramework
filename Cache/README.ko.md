# Cache Server

고성능 캐시 서버입니다. Redis 기반으로 게임 데이터와 세션 정보를 효율적으로 캐시합니다.

## 🌍 언어 선택

- [English](README.md) (기본)
- [한국어](README.ko.md)

## 🎯 주요 기능

### 캐시 관리
- **Redis 기반**: 고성능 인메모리 캐시
- **세션 관리**: 사용자 세션 정보 캐시
- **게임 데이터 캐시**: 스킬, 아이템, 몬스터 데이터 캐시

### 채널 관리
- **Channel.cs**: 채널 정보 관리
- **ChannelLoader.cs**: 채널 데이터 로딩
- **ChannelUpdater.cs**: 채널 데이터 업데이트

### 구독 시스템
- **Subscribe.cs**: Redis Pub/Sub 구독 관리
- **실시간 업데이트**: 데이터 변경 시 실시간 알림
- **이벤트 처리**: 캐시 이벤트 처리

## 🏗️ 아키텍처

### 핵심 컴포넌트
- **Cache.cs**: 메인 캐시 관리자
- **CacheThread.cs**: 캐시 처리 스레드
- **Subscribe.cs**: 구독 관리
- **Channel/**: 채널 관련 컴포넌트

### 캐시 구조
```csharp
public class Cache {
    // Redis 연결 관리
    private IConnectionMultiplexer redis;
    
    // 세션 캐시
    private IDatabase sessionCache;
    
    // 게임 데이터 캐시
    private IDatabase gameDataCache;
    
    // 채널 캐시
    private IDatabase channelCache;
}
```

## 📊 사용 예시

### 세션 캐시
```csharp
// 세션 저장
await cache.SetSessionAsync(userId, sessionData);

// 세션 조회
var session = await cache.GetSessionAsync(userId);

// 세션 삭제
await cache.RemoveSessionAsync(userId);
```

### 게임 데이터 캐시
```csharp
// 스킬 데이터 캐시
await cache.SetSkillDataAsync(skillId, skillData);

// 아이템 데이터 캐시
await cache.SetItemDataAsync(itemId, itemData);

// 몬스터 데이터 캐시
await cache.SetMonsterDataAsync(monsterId, monsterData);
```

### 채널 관리
```csharp
// 채널 정보 로드
var channel = await channelLoader.LoadChannelAsync(channelId);

// 채널 정보 업데이트
await channelUpdater.UpdateChannelAsync(channelId, channelData);

// 채널 구독
await subscribe.SubscribeToChannelAsync(channelId, callback);
```

## 📁 프로젝트 구조

```
Cache/
├── Cache.csproj             # 프로젝트 파일
├── Cache.cs                 # 메인 캐시 클래스
├── CacheThread.cs           # 캐시 처리 스레드
├── Subscribe.cs             # 구독 관리
├── Channel/                 # 채널 관련
│   ├── Channel.cs           # 채널 클래스
│   ├── ChannelLoader.cs     # 채널 로더
│   └── ChannelUpdater.cs    # 채널 업데이터
└── bin/                     # 빌드 출력
```

## 🔧 개발 환경

### 요구사항
- .NET 6.0 이상
- Redis 서버
- StackExchange.Redis

### 의존성
- Redis 클라이언트
- JSON 직렬화
- 비동기 처리

## 📈 성능 최적화

### 메모리 관리
- **LRU 캐시**: 최근 사용된 데이터 우선 보관
- **메모리 압축**: Redis 압축 기능 활용
- **TTL 설정**: 자동 만료 시간 설정

### 처리 최적화
- **비동기 처리**: 모든 캐시 작업 비동기 처리
- **배치 처리**: 다중 데이터 동시 처리
- **연결 풀링**: Redis 연결 풀 관리

## 🔗 관련 프로젝트

- **[Battle/](../Battle/README.md)** - 배틀 서버 (세션 캐시)
- **[Lobby/](../Lobby/README.md)** - 로비 서버 (매칭 캐시)
- **[Chat/](../Chat/README.md)** - 채팅 서버 (메시지 캐시)
- **[GameData/](../GameData/README.md)** - 게임 데이터 (데이터 캐시) 
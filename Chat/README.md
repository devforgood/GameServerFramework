# Chat Server

실시간 채팅 서버입니다. gRPC 기반으로 실시간 메시지 전송과 채널 관리 기능을 제공합니다.

## 🎯 주요 기능

### 실시간 채팅
- **gRPC 스트리밍**: 실시간 양방향 메시지 전송
- **채널 관리**: 공개/비공개 채널 생성 및 관리
- **메시지 브로드캐스트**: 채널 내 모든 사용자에게 메시지 전송

### OAuth 인증
- **Facebook 인증**: Facebook OAuth 지원
- **GameCenter 인증**: Apple GameCenter 인증 지원
- **외부 인증 제공자**: 확장 가능한 인증 시스템

### 채팅 기능
- **개인 메시지**: 1:1 개인 메시지
- **그룹 채팅**: 다중 사용자 그룹 채팅
- **시스템 메시지**: 서버에서 전송하는 시스템 알림

## 🏗️ 아키텍처

### 핵심 컴포넌트
- **ChatService**: gRPC 채팅 서비스
- **ChannelManager**: 채널 관리
- **UserManager**: 사용자 관리
- **MessageQueue**: 메시지 큐 관리

### 통신 프로토콜
```protobuf
service ChatService {
    rpc JoinChannel(JoinChannelRequest) returns (JoinChannelResponse);
    rpc LeaveChannel(LeaveChannelRequest) returns (LeaveChannelResponse);
    rpc SendMessage(SendMessageRequest) returns (SendMessageResponse);
    rpc GetChannelMessages(GetChannelMessagesRequest) returns (GetChannelMessagesResponse);
    rpc StreamMessages(StreamMessagesRequest) returns (stream ChatMessage);
}
```

## 📊 사용 예시

### 채널 참여
```csharp
var request = new JoinChannelRequest {
    ChannelId = "general",
    UserId = "user123"
};

var response = await chatClient.JoinChannelAsync(request);
```

### 메시지 전송
```csharp
var message = new SendMessageRequest {
    ChannelId = "general",
    UserId = "user123",
    Content = "Hello, World!",
    MessageType = MessageType.Text
};

var response = await chatClient.SendMessageAsync(message);
```

### 실시간 메시지 스트리밍
```csharp
using var call = chatClient.StreamMessages(new StreamMessagesRequest {
    ChannelId = "general",
    UserId = "user123"
});

await foreach (var message in call.ResponseStream) {
    Console.WriteLine($"{message.UserId}: {message.Content}");
}
```

## 📁 프로젝트 구조

```
Chat/
├── Chat.csproj              # 프로젝트 파일
├── appsettings.json         # 기본 설정
├── appsettings.Release.json # 배포 설정
├── build.sh                 # 빌드 스크립트
├── GrpcHostedService.cs     # gRPC 호스팅 서비스
├── Service/
│   └── ChatService.cs       # 채팅 서비스 구현
├── OAuth/                   # OAuth 인증
│   ├── ExternalProvider.cs
│   ├── ExternalProviderFacebook.cs
│   └── ExternalProviderGameCenter.cs
├── Credentials/             # 인증 정보
│   └── README
└── bin/                     # 빌드 출력
```

## 🔧 개발 환경

### 요구사항
- .NET 6.0 이상
- gRPC
- Redis (선택사항, 캐시용)

### 의존성
- gRPC 통신
- OAuth 인증 제공자
- 메시지 큐 시스템

## 📈 성능 최적화

### 메시지 처리
- **비동기 처리**: 메시지 전송 비동기 처리
- **배치 처리**: 다중 메시지 동시 처리
- **메모리 최적화**: 효율적인 메시지 구조

### 확장성
- **수평 확장**: 다중 서버 인스턴스 지원
- **로드 밸런싱**: gRPC 로드 밸런서 지원
- **캐시 시스템**: Redis 기반 메시지 캐시

## 🔗 관련 프로젝트

- **[Lobby/](../Lobby/README.md)** - 로비 서버 (사용자 인증)
- **[Battle/](../Battle/README.md)** - 배틀 서버 (인게임 채팅)
- **[Cache/](../Cache/README.md)** - 캐시 서버
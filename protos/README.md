# Protocol Buffers

gRPC 프로토콜 정의 파일들입니다. 서버 간 통신을 위한 메시지 구조와 서비스 정의를 포함합니다.

## 🎯 주요 기능

### 프로토콜 정의
- **chat.proto**: 채팅 서비스 프로토콜
- **dummycontrol.proto**: 더미 컨트롤 프로토콜
- **gRPC 서비스**: 서버 간 통신을 위한 서비스 정의

### 메시지 구조
- **요청/응답 메시지**: 클라이언트-서버 간 통신 메시지
- **스트리밍 메시지**: 실시간 데이터 전송
- **이벤트 메시지**: 서버 간 이벤트 통신

## 📁 프로젝트 구조

```
protos/
├── chat.proto               # 채팅 서비스 프로토콜
├── dummycontrol.proto       # 더미 컨트롤 프로토콜
└── grpc_csharp_plugin.exe   # gRPC C# 플러그인
```

## 📊 프로토콜 예시

### 채팅 서비스
```protobuf
service ChatService {
    rpc JoinChannel(JoinChannelRequest) returns (JoinChannelResponse);
    rpc LeaveChannel(LeaveChannelRequest) returns (LeaveChannelResponse);
    rpc SendMessage(SendMessageRequest) returns (SendMessageResponse);
    rpc StreamMessages(StreamMessagesRequest) returns (stream ChatMessage);
}

message ChatMessage {
    string user_id = 1;
    string channel_id = 2;
    string content = 3;
    int64 timestamp = 4;
    MessageType type = 5;
}
```

### 더미 컨트롤
```protobuf
service DummyControlService {
    rpc SendCommand(DummyCommand) returns (DummyResponse);
    rpc GetStatus(DummyStatusRequest) returns (DummyStatusResponse);
}

message DummyCommand {
    string command = 1;
    map<string, string> parameters = 2;
}
```

## 🔧 개발 환경

### 요구사항
- Protocol Buffers 컴파일러
- gRPC 도구
- C# 플러그인

### 코드 생성
```bash
# C# 코드 생성
protoc --csharp_out=./generated --grpc_out=./generated --plugin=protoc-gen-grpc=grpc_csharp_plugin.exe chat.proto

# C++ 코드 생성
protoc --cpp_out=./generated --grpc_out=./generated --plugin=protoc-gen-grpc=grpc_cpp_plugin chat.proto
```

## 🔗 관련 프로젝트

- **[Chat/](../Chat/README.md)** - 채팅 서버 (chat.proto 사용)
- **[Battle/](../Battle/README.md)** - 배틀 서버 (프로토콜 사용)
- **[Lobby/](../Lobby/README.md)** - 로비 서버 (프로토콜 사용) 
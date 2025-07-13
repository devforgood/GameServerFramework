# FlatBuffers

FlatBuffers 직렬화 라이브러리입니다. 고성능 바이너리 직렬화를 통해 빠른 데이터 전송을 제공합니다.

## 🎯 주요 기능

### 직렬화 시스템
- **바이너리 직렬화**: 효율적인 바이너리 형식
- **스키마 정의**: .fbs 파일을 통한 데이터 구조 정의
- **코드 생성**: C++, C#, Java 등 다중 언어 지원

### 성능 최적화
- **제로 카피**: 메모리 복사 없이 데이터 접근
- **빠른 파싱**: 효율적인 데이터 파싱
- **메모리 효율성**: 최소한의 메모리 사용

## 📁 프로젝트 구조

```
flatbuffer/
├── flatbuffers-1.12.0/      # FlatBuffers 라이브러리
│   ├── include/              # 헤더 파일
│   ├── src/                  # 소스 코드
│   ├── samples/              # 샘플 코드
│   └── tests/                # 테스트 코드
├── flatc.bat                 # Windows 컴파일러 배치 파일
├── flatc.exe                 # FlatBuffers 컴파일러
├── lib/                      # 라이브러리 파일
│   ├── flatbuffers.lib       # Windows 라이브러리
│   └── flatbuffers_r.lib     # Release 라이브러리
└── syncnet.fbs               # 네트워크 프로토콜 정의
```

## 📊 사용 예시

### 스키마 정의
```flatbuffers
// syncnet.fbs
table Player {
  id:int;
  name:string;
  position:Vec3;
  health:int;
  level:int;
}

table Vec3 {
  x:float;
  y:float;
  z:float;
}

root_type Player;
```

### 코드 생성
```bash
# C++ 코드 생성
flatc --cpp syncnet.fbs

# C# 코드 생성
flatc --csharp syncnet.fbs
```

### 사용 예시
```cpp
// 데이터 생성
flatbuffers::FlatBufferBuilder builder;
auto name = builder.CreateString("Player1");
auto position = CreateVec3(builder, 100.0f, 200.0f, 0.0f);
auto player = CreatePlayer(builder, 1, name, position, 100, 10);
builder.Finish(player);

// 데이터 접근
auto player_data = flatbuffers::GetRoot<Player>(builder.GetBufferPointer());
std::string name = player_data->name()->str();
float x = player_data->position()->x();
```

## 🔧 개발 환경

### 요구사항
- C++11 이상
- CMake 빌드 시스템
- 다중 플랫폼 지원

### 의존성
- 표준 C++ 라이브러리
- 플랫폼별 시스템 라이브러리

## 📈 성능 최적화

### 메모리 관리
- **제로 카피**: 메모리 복사 없이 직접 접근
- **압축**: 효율적인 데이터 압축
- **풀링**: 메모리 풀을 통한 할당 최적화

### 처리 최적화
- **인라인 접근**: 직접 메모리 접근으로 빠른 파싱
- **캐시 친화적**: CPU 캐시에 최적화된 데이터 구조
- **병렬 처리**: 다중 스레드 직렬화/역직렬화

## 🔗 관련 프로젝트

- **[GameData/](../GameData/README.md)** - 게임 데이터 (직렬화 사용)
- **[Battle/](../Battle/README.md)** - 배틀 서버 (네트워크 프로토콜)
- **[Client/](../Client/README.md)** - Unity 클라이언트 (데이터 전송) 
# Data Models

게임 서버에서 사용하는 데이터 모델들입니다. 엔티티 프레임워크를 사용한 데이터베이스 모델과 비즈니스 로직 모델을 포함합니다.

## 🎯 주요 기능

### 데이터 모델
- **AdvertisementReward**: 광고 보상 모델
- **BannedWord**: 금지어 모델
- **사용자 모델**: 플레이어, 계정 정보
- **게임 데이터 모델**: 아이템, 스킬, 퀘스트 등

### 확장 기능
- **모델 확장**: 기존 모델의 기능 확장
- **비즈니스 로직**: 도메인 로직 포함
- **검증 로직**: 데이터 유효성 검증

## 📁 프로젝트 구조

```
Models/
├── Models.csproj             # 프로젝트 파일
├── AdvertisementReward.cs    # 광고 보상 모델
├── AdvertisementReward.Extend.cs # 광고 보상 확장
├── BannedWord.cs             # 금지어 모델
├── Enum/                     # 열거형 정의
├── Shard/                    # 샤딩 관련 모델
└── obj/                      # 빌드 출력
```

## 📊 모델 예시

### 광고 보상 모델
```csharp
public class AdvertisementReward {
    public int Id { get; set; }
    public string UserId { get; set; }
    public string AdType { get; set; }
    public int RewardAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsClaimed { get; set; }
}

public static class AdvertisementRewardExtensions {
    public static bool CanClaim(this AdvertisementReward reward) {
        return !reward.IsClaimed && reward.CreatedAt.AddHours(24) > DateTime.UtcNow;
    }
}
```

### 금지어 모델
```csharp
public class BannedWord {
    public int Id { get; set; }
    public string Word { get; set; }
    public string Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
```

## 🔧 개발 환경

### 요구사항
- .NET 6.0 이상
- Entity Framework Core
- 데이터베이스 (SQL Server/MySQL)

### 의존성
- Entity Framework Core
- System.ComponentModel.DataAnnotations
- Newtonsoft.Json

## 🔗 관련 프로젝트

- **[Lobby/](../Lobby/README.md)** - 로비 서버 (사용자 모델)
- **[Chat/](../Chat/README.md)** - 채팅 서버 (금지어 모델)
- **[GmTool/](../GmTool/README.md)** - GM 도구 (모델 관리) 
# GM Tool

게임 마스터(GM) 도구입니다. 웹 기반으로 게임 서버를 관리하고 모니터링할 수 있습니다.

## 🌍 언어 선택

- [English](README.md) (기본)
- [한국어](README.ko.md)

## 🎯 주요 기능

### 게임 관리
- **플레이어 관리**: 플레이어 정보 조회 및 수정
- **서버 상태 모니터링**: 각 서버의 상태 및 성능 모니터링
- **게임 데이터 관리**: 아이템, 스킬, 몬스터 데이터 관리

### 관리자 기능
- **사용자 관리**: 계정 생성, 수정, 삭제
- **권한 관리**: GM 권한 설정 및 관리
- **로그 조회**: 게임 로그 및 시스템 로그 조회

### 통계 및 분석
- **게임 통계**: 플레이어 수, 매칭 성공률 등
- **성능 분석**: 서버 성능 및 응답 시간 분석
- **이벤트 관리**: 게임 이벤트 생성 및 관리

## 📁 프로젝트 구조

```
GmTool/
├── GmTool.csproj             # 프로젝트 파일
├── appsettings.json          # 기본 설정
├── appsettings.Development.json # 개발 환경 설정
├── appsettings.KakaoDevQa.json # 카카오 개발 환경 설정
├── Areas/                    # 영역별 기능
│   └── Identity/            # 인증 관련
├── Data/                     # 데이터 액세스
│   └── ApplicationDbContext.cs
├── MailBox/                  # 메일 시스템
│   ├── MailBox.cs
│   └── send.request.cs
├── Migrations/               # 데이터베이스 마이그레이션
├── Models/                   # 데이터 모델
├── Modules/                  # 비즈니스 모듈
├── Pages/                    # Razor 페이지
├── Ranking/                  # 랭킹 시스템
├── Services/                 # 서비스 레이어
├── UnitTest/                 # 단위 테스트
├── WebAPIClient/            # 웹 API 클라이언트
└── wwwroot/                 # 정적 파일
```

## 🔧 개발 환경

### 요구사항
- .NET 6.0 이상
- ASP.NET Core
- Entity Framework Core
- 데이터베이스 (SQL Server/MySQL)

### 의존성
- ASP.NET Core Identity
- Entity Framework Core
- SignalR (실시간 업데이트)

## 🔗 관련 프로젝트

- **[Battle/](../Battle/README.md)** - 배틀 서버 (게임 상태 모니터링)
- **[Lobby/](../Lobby/README.md)** - 로비 서버 (플레이어 관리)
- **[Cache/](../Cache/README.md)** - 캐시 서버 (데이터 캐시) 
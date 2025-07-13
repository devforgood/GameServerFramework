# IAP (In-App Purchase) Server

인앱 결제 서버입니다. Google Play, Apple App Store 등의 결제 시스템을 지원합니다.

## 🎯 주요 기능

### 결제 처리
- **Google Play 결제**: Google Play Store 결제 검증
- **Apple App Store 결제**: iOS 앱스토어 결제 검증
- **영수증 검증**: 서버 사이드 영수증 검증
- **결제 완료 처리**: 아이템 지급 및 상태 업데이트

### 보안
- **서명 검증**: 결제 영수증 서명 검증
- **중복 결제 방지**: 동일 결제 중복 처리 방지
- **환불 처리**: 환불 요청 처리

## 📁 프로젝트 구조

```
IAP/
├── IAP.csproj               # 프로젝트 파일
├── InAppPurchase.cs         # 인앱 결제 클래스
├── GooglePlayReceipt.cs     # Google Play 영수증 처리
└── bin/                     # 빌드 출력
```

## 🔧 개발 환경

### 요구사항
- .NET 6.0 이상
- Google Play Developer API
- Apple App Store Connect API

### 의존성
- Google.Apis.AndroidPublisher
- Apple App Store Connect API
- JSON 웹 토큰 (JWT)

## 🔗 관련 프로젝트

- **[Lobby/](../Lobby/README.md)** - 로비 서버 (결제 후 아이템 지급)
- **[Cache/](../Cache/README.md)** - 캐시 서버 (결제 정보 캐시) 
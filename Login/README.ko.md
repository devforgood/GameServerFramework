# Login Server

사용자 로그인 및 인증을 담당하는 서버입니다. 웹 기반 로그인 시스템을 제공합니다.

## 🌍 언어 선택

- [English](README.md) (기본)
- [한국어](README.ko.md)

## 🎯 주요 기능

### 로그인 처리
- **사용자 인증**: 아이디/비밀번호 기반 로그인
- **세션 관리**: 로그인 세션 생성 및 관리
- **보안**: 암호화된 비밀번호 검증

### 웹 인터페이스
- **로그인 페이지**: 사용자 로그인 폼
- **관리자 페이지**: 서버 상태 및 사용자 관리
- **RESTful API**: 클라이언트와의 통신

## 📁 프로젝트 구조

```
Login/
├── Login.csproj              # 프로젝트 파일
├── appsettings.json          # 기본 설정
├── appsettings.Development.json # 개발 환경 설정
├── login.conf                # 로그인 설정
├── Controllers/              # MVC 컨트롤러
├── Models/                   # 데이터 모델
├── Views/                    # 뷰 템플릿
├── wwwroot/                  # 정적 파일
└── Properties/               # 프로젝트 속성
```

## 🔧 개발 환경

### 요구사항
- .NET 6.0 이상
- ASP.NET Core
- 데이터베이스 (SQL Server/MySQL)

### 의존성
- Entity Framework Core
- ASP.NET Core Identity
- JWT 토큰 인증

## 🔗 관련 프로젝트

- **[Lobby/](../Lobby/README.md)** - 로비 서버 (인증 후 연결)
- **[Cache/](../Cache/README.md)** - 캐시 서버 (세션 캐시) 
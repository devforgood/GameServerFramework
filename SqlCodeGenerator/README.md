# SQL Code Generator

SQL 코드 자동 생성 도구입니다. XML 스키마를 기반으로 데이터베이스 쿼리와 모델 코드를 자동 생성합니다.

## 🎯 주요 기능

### 코드 생성
- **SQL 쿼리 생성**: CRUD 작업을 위한 SQL 쿼리 자동 생성
- **모델 코드 생성**: 데이터베이스 테이블에 대응하는 모델 클래스 생성
- **리포지토리 패턴**: 데이터 액세스 레이어 코드 생성

### 스키마 기반
- **XML 스키마**: 테이블 구조를 XML로 정의
- **템플릿 기반**: Jinja2 템플릿을 사용한 코드 생성
- **다중 언어**: C#, C++, Java 등 다중 언어 지원

## 📁 프로젝트 구조

```
SqlCodeGenerator/
├── SqlCodeGenerator.pyproj   # Python 프로젝트 파일
├── generate.py               # 메인 생성 스크립트
├── schema.xml                # 데이터베이스 스키마 정의
└── templates/                # 코드 생성 템플릿
    ├── Base.h.j2            # C++ 헤더 템플릿
    ├── Factory.cpp.j2       # C++ 팩토리 템플릿
    └── Factory.cs.j2        # C# 팩토리 템플릿
```

## 📊 스키마 예시

### XML 스키마 정의
```xml
<?xml version="1.0" encoding="UTF-8"?>
<database>
    <table name="Player">
        <column name="Id" type="int" primary="true" auto_increment="true"/>
        <column name="Name" type="varchar(50)" nullable="false"/>
        <column name="Level" type="int" default="1"/>
        <column name="Experience" type="bigint" default="0"/>
        <column name="CreatedAt" type="datetime" default="CURRENT_TIMESTAMP"/>
    </table>
    
    <table name="Item">
        <column name="Id" type="int" primary="true" auto_increment="true"/>
        <column name="PlayerId" type="int" foreign_key="Player.Id"/>
        <column name="ItemType" type="varchar(20)" nullable="false"/>
        <column name="Quantity" type="int" default="1"/>
    </table>
</database>
```

## 🚀 사용 방법

### 1. 스키마 정의
```xml
<!-- schema.xml -->
<database>
    <table name="User">
        <column name="Id" type="int" primary="true"/>
        <column name="Username" type="varchar(50)"/>
        <column name="Email" type="varchar(100)"/>
    </table>
</database>
```

### 2. 코드 생성
```bash
# Python 스크립트 실행
python generate.py --schema schema.xml --output ./generated --language csharp
```

### 3. 생성된 코드
```csharp
// User.cs
public class User {
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
}

// UserRepository.cs
public class UserRepository {
    public async Task<User> GetByIdAsync(int id) {
        // 자동 생성된 SQL 쿼리
        const string sql = "SELECT Id, Username, Email FROM User WHERE Id = @Id";
        // 구현...
    }
}
```

## 🔧 개발 환경

### 요구사항
- Python 3.7 이상
- Jinja2 템플릿 엔진
- XML 파싱 라이브러리

### 의존성
- jinja2
- xml.etree.ElementTree
- argparse

## 📈 기능 확장

### 새로운 언어 지원
1. `templates/` 폴더에 새로운 템플릿 추가
2. `generate.py`에 언어 옵션 추가
3. 해당 언어의 코드 생성 로직 구현

### 새로운 테이블 타입
1. `schema.xml`에 새로운 테이블 정의 추가
2. 필요한 경우 템플릿 수정
3. 생성 로직 업데이트

## 🔗 관련 프로젝트

- **[GameData/](../GameData/README.md)** - 게임 데이터 (생성된 모델 사용)
- **[Models/](../Models/README.md)** - 데이터 모델 (생성된 코드)
- **[Game/](../Game/README.md)** - 게임 로직 (데이터 액세스) 
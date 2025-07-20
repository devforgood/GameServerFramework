# GM Tool

Game Master (GM) tool. Web-based game server management and monitoring system.

## 🌍 Language Selection

- [English](README.md) (Default)
- [한국어](README.ko.md)

## 🎯 Key Features

### Game Management
- **Player Management**: Player information lookup and modification
- **Server Status Monitoring**: Status and performance monitoring of each server
- **Game Data Management**: Item, skill, monster data management

### Administrator Functions
- **User Management**: Account creation, modification, deletion
- **Permission Management**: GM permission settings and management
- **Log Viewing**: Game logs and system logs viewing

### Statistics and Analysis
- **Game Statistics**: Player count, matching success rate, etc.
- **Performance Analysis**: Server performance and response time analysis
- **Event Management**: Game event creation and management

## 📁 Project Structure

```
GmTool/
├── GmTool.csproj             # Project file
├── appsettings.json          # Default configuration
├── appsettings.Development.json # Development environment configuration
├── appsettings.KakaoDevQa.json # Kakao development environment configuration
├── Areas/                    # Area-specific features
│   └── Identity/            # Authentication related
├── Data/                     # Data access
│   └── ApplicationDbContext.cs
├── MailBox/                  # Mail system
│   ├── MailBox.cs
│   └── send.request.cs
├── Migrations/               # Database migrations
├── Models/                   # Data models
├── Modules/                  # Business modules
├── Pages/                    # Razor pages
├── Ranking/                  # Ranking system
├── Services/                 # Service layer
├── UnitTest/                 # Unit tests
├── WebAPIClient/            # Web API client
└── wwwroot/                 # Static files
```

## 🔧 Development Environment

### Requirements
- .NET 6.0 or higher
- ASP.NET Core
- Entity Framework Core
- Database (SQL Server/MySQL)

### Dependencies
- ASP.NET Core Identity
- Entity Framework Core
- SignalR (real-time updates)

## 🔗 Related Projects

- **[Battle/](../Battle/README.md)** - Battle server (game status monitoring)
- **[Lobby/](../Lobby/README.md)** - Lobby server (player management)
- **[Cache/](../Cache/README.md)** - Cache server (data cache) 
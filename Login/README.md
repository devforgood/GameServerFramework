# Login Server

Server responsible for user login and authentication. Provides web-based login system.

## 🌍 Language Selection

- [English](README.md) (Default)
- [한국어](README.ko.md)

## 🎯 Key Features

### Login Processing
- **User Authentication**: ID/password-based login
- **Session Management**: Login session creation and management
- **Security**: Encrypted password verification

### Web Interface
- **Login Page**: User login form
- **Admin Page**: Server status and user management
- **RESTful API**: Communication with clients

## 📁 Project Structure

```
Login/
├── Login.csproj              # Project file
├── appsettings.json          # Default configuration
├── appsettings.Development.json # Development environment configuration
├── login.conf                # Login configuration
├── Controllers/              # MVC controllers
├── Models/                   # Data models
├── Views/                    # View templates
├── wwwroot/                  # Static files
└── Properties/               # Project properties
```

## 🔧 Development Environment

### Requirements
- .NET 6.0 or higher
- ASP.NET Core
- Database (SQL Server/MySQL)

### Dependencies
- Entity Framework Core
- ASP.NET Core Identity
- JWT token authentication

## 🔗 Related Projects

- **[Lobby/](../Lobby/README.md)** - Lobby server (connection after authentication)
- **[Cache/](../Cache/README.md)** - Cache server (session cache) 
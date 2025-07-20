# IAP (In-App Purchase) Server

In-app purchase server. Supports payment systems like Google Play, Apple App Store, etc.

## 🌍 Language Selection

- [English](README.md) (Default)
- [한국어](README.ko.md)

## 🎯 Key Features

### Payment Processing
- **Google Play Payments**: Google Play Store payment verification
- **Apple App Store Payments**: iOS App Store payment verification
- **Receipt Verification**: Server-side receipt verification
- **Payment Completion**: Item delivery and status updates

### Security
- **Signature Verification**: Payment receipt signature verification
- **Duplicate Payment Prevention**: Prevent duplicate processing of same payment
- **Refund Processing**: Refund request handling

## 📁 Project Structure

```
IAP/
├── IAP.csproj               # Project file
├── InAppPurchase.cs         # In-app purchase class
├── GooglePlayReceipt.cs     # Google Play receipt processing
└── bin/                     # Build output
```

## 🔧 Development Environment

### Requirements
- .NET 6.0 or higher
- Google Play Developer API
- Apple App Store Connect API

### Dependencies
- Google.Apis.AndroidPublisher
- Apple App Store Connect API
- JSON Web Tokens (JWT)

## 🔗 Related Projects

- **[Lobby/](../Lobby/README.md)** - Lobby server (item delivery after payment)
- **[Cache/](../Cache/README.md)** - Cache server (payment information cache) 
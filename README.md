# 📸 InstagramAPI - ASP.NET Core 8 + Onion Architecture

A full-featured Instagram clone REST API built with **ASP.NET Core 8**, **SQL Server**, **Onion Architecture**, and **Generic Repository Pattern**.

---

## 🏗️ Architecture

```
InstagramApi/
├── src/
│   ├── Core/                          ← Domain + Application (innermost layers)
│   │   ├── InstagramApi.Domain/       ← Entities, Enums, Common base classes
│   │   └── InstagramApi.Application/  ← DTOs, Interfaces, Validators, AutoMapper
│   │
│   ├── Infrastructure/                ← External concerns
│   │   ├── InstagramApi.Infrastructure/ ← Repositories, Services (Auth, Email, File, Cache)
│   │   └── InstagramApi.Persistence/  ← EF Core DbContext, Configurations, Migrations
│   │
│   └── Presentation/
│       └── InstagramApi.API/          ← Controllers, Middleware, Program.cs
│
└── tests/
    ├── InstagramApi.UnitTests/
    └── InstagramApi.IntegrationTests/
```

### Dependency Rule (Onion Architecture)
```
API → Application ← Infrastructure
         ↑
       Domain
```
- **Domain** has no dependencies
- **Application** depends only on Domain
- **Infrastructure** depends on Application
- **API** depends on Application + Infrastructure

---

## 🚀 Features

| Feature | Details |
|---|---|
| 🔐 **Authentication** | JWT Access Token + Refresh Token |
| 📧 **Email** | Confirmation, Password Reset, Welcome Email (MailKit) |
| 🖼️ **Media** | Upload images/videos with ImageSharp processing |
| 📱 **Posts** | Create, Edit, Delete, Like, Save, Share |
| 💬 **Comments** | Nested replies, likes |
| 📖 **Stories** | 24h expiry, view tracking |
| 👥 **Follow System** | Public/Private accounts, follow requests |
| 🔍 **Search** | Users, hashtags |
| 📩 **Direct Messages** | 1:1 conversations, unsend |
| 🔔 **Notifications** | Like, comment, follow, DM |
| 🛡️ **Admin Panel** | Dashboard, user management, reports |
| 🚫 **Block/Report** | Block users, report content |
| ♻️ **Soft Delete** | All entities support soft delete |
| 📄 **Pagination** | Generic paged results |
| 🗃️ **Generic Repository** | Full CRUD + paged query |
| 🔄 **Unit of Work** | Transaction management |
| ✅ **Validation** | FluentValidation |
| 🗺️ **AutoMapper** | Full mapping profiles |

---

## ⚡ Quick Start

### Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB works)
- (Optional) Redis for distributed caching

### 1. Clone & Configure
```bash
git clone <repo>
cd InstagramApi
```

Edit `src/Presentation/InstagramApi.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InstagramApiDb;..."
  },
  "Jwt": {
    "Secret": "YourSecretKeyMin32Chars!"
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "Username": "you@gmail.com",
    "Password": "your-app-password"
  }
}
```

### 2. Run Migrations
```bash
cd src/Presentation/InstagramApi.API
dotnet ef migrations add InitialCreate --project ../../Infrastructure/InstagramApi.Persistence
dotnet ef database update --project ../../Infrastructure/InstagramApi.Persistence
```

### 3. Run the API
```bash
dotnet run --project src/Presentation/InstagramApi.API
```

Open `https://localhost:7001` → Swagger UI

---

## 🔑 Default Admin Account
```
Email:    admin@instagramapi.com
Password: Admin@123456
Role:     SuperAdmin
```

---

## 📡 API Endpoints

### Auth
| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/login` | Login |
| POST | `/api/auth/refresh-token` | Refresh JWT |
| POST | `/api/auth/logout` | Logout |
| POST | `/api/auth/forgot-password` | Send reset email |
| POST | `/api/auth/reset-password` | Reset password |
| GET | `/api/auth/confirm-email` | Confirm email |
| POST | `/api/auth/change-password` | Change password |
| GET | `/api/auth/me` | Current user info |

### Users
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/users/{username}` | Get profile |
| PUT | `/api/users/profile` | Update profile |
| POST | `/api/users/profile/avatar` | Upload avatar |
| GET | `/api/users/search?q=` | Search users |
| GET | `/api/users/suggestions` | Suggested users |
| POST | `/api/users/{userId}/follow` | Follow user |
| DELETE | `/api/users/{userId}/follow` | Unfollow |
| GET | `/api/users/{userId}/followers` | Followers list |
| GET | `/api/users/{userId}/following` | Following list |
| POST | `/api/users/{userId}/block` | Block user |
| POST | `/api/users/follow-requests/{id}/accept` | Accept request |

### Posts
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/posts/feed` | Home feed |
| GET | `/api/posts/explore` | Explore page |
| POST | `/api/posts` | Create post |
| GET | `/api/posts/{postId}` | Get post |
| PUT | `/api/posts/{postId}` | Update post |
| DELETE | `/api/posts/{postId}` | Delete post |
| POST | `/api/posts/{postId}/like` | Like post |
| DELETE | `/api/posts/{postId}/like` | Unlike post |
| POST | `/api/posts/{postId}/save` | Save post |
| GET | `/api/posts/saved` | Saved posts |
| GET | `/api/posts/hashtag/{tag}` | Posts by hashtag |

### Comments
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/posts/{postId}/comments` | Get comments |
| POST | `/api/posts/{postId}/comments` | Add comment |
| DELETE | `/api/comments/{commentId}` | Delete comment |
| POST | `/api/comments/{commentId}/like` | Like comment |
| GET | `/api/comments/{commentId}/replies` | Get replies |

### Stories
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/stories/feed` | Story feed |
| POST | `/api/stories` | Create story |
| DELETE | `/api/stories/{storyId}` | Delete story |
| POST | `/api/stories/{storyId}/view` | View story |

### Messages
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/messages/conversations` | All conversations |
| GET | `/api/messages/conversations/{id}/messages` | Messages |
| POST | `/api/messages/send` | Send message |
| DELETE | `/api/messages/{messageId}` | Unsend message |

### Notifications
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/notifications` | Get notifications |
| PATCH | `/api/notifications/read-all` | Mark all read |
| GET | `/api/notifications/unread-count` | Unread count |

### Admin
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/admin/dashboard` | Dashboard stats |
| GET | `/api/admin/users` | All users |
| PATCH | `/api/admin/users/{id}` | Update user |
| POST | `/api/admin/users/{id}/ban` | Ban user |
| GET | `/api/admin/reports` | Pending reports |
| PATCH | `/api/admin/reports/{id}` | Resolve report |
| GET | `/api/admin/settings` | App settings |

---

## 📦 NuGet Packages

### Application Layer
- `AutoMapper` + `AutoMapper.Extensions.Microsoft.DependencyInjection`
- `FluentValidation` + `FluentValidation.DependencyInjectionExtensions`
- `MediatR`

### Infrastructure Layer
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.IdentityModel.Tokens` + `System.IdentityModel.Tokens.Jwt`
- `MailKit` — Email sending
- `SixLabors.ImageSharp` — Image processing
- `StackExchange.Redis` — Distributed caching

### API Layer
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Swashbuckle.AspNetCore` + Annotations
- `Serilog.AspNetCore` + Sinks (Console, File, MSSqlServer)
- `AspNetCoreRateLimit`

---

## 🗄️ Database Schema

Key tables: `Users`, `Posts`, `PostMedia`, `Comments`, `Likes`, `CommentLikes`, `Follows`, `Stories`, `StoryViews`, `Hashtags`, `PostHashtags`, `PostTags`, `SavedPosts`, `Messages`, `Conversations`, `Notifications`, `Reports`, `BlockedUsers`, `AppSettings`

---

## 🔒 Security

- JWT with short-lived access tokens (1h) + long-lived refresh tokens (30d)
- Password hashing via ASP.NET Core Identity (PBKDF2)
- Account lockout after 5 failed attempts
- Soft delete for all data (GDPR-friendly)
- Input validation with FluentValidation
- Global exception handling middleware

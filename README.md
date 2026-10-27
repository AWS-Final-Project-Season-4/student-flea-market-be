⚙️ STUDENT FLEA MARKET - BACKEND WEB API

An enterprise-ready, robust RESTful Web API powering the Student Flea Market platform. Build with .NET 8, Entity Framework Core and PostgreSQL, engineered to handle high-concurrency requests and integrate directly with AWS Cloud Infrastructure.

--------------------------------

🚀 Tech Stack & Core Libraries
- Runtime & Framework: .NET 8 Web API
- Database & Engine: PostgreSQL
- ORM & Data Access: Entity Framework Core 8
- Authentication & Security: JWT (JSON Web Tokens) with Refresh Token rotation, ASP.NET Core Identity / Password Hashing
- Object Mapping: Mapster (or AutoMapper)
- Validation: FluentValidation
- API Documentation: Swagger UI
- Cloud SDK: AWS SDK for .NET (S3, CloudFront utilities)
- Logging & Diagnostics: SeriLog
- Containerization: Docker & Docker Compose

-----------------------------------

☁️ AWS Cloud & Architecture Highlights
- Direct S3 Upload Offloading: Backend generates cryptographically signed S3 Presigned URLs, allowing clients to upload assets straight to Amazon S3 without saturating backend memory or network bandwidth.
- Clean Architecture Principles: Strict separation of concerns across Domain, Application, Infrastructure, and API layers to ensure testability and maintainability.
- Enterprise Resilience: Centralized Global Exception Middleware, standard API response wrappers, and secure environment configuration handling.

---

## 🔐 Feature 1 — Authentication & Authorization Profile

### 1.1 Cấu trúc mã nguồn

| Layer | Thành phần mới |
|-------|----------------|
| **Domain** | `ApplicationUser` (mở rộng), `RefreshToken`, `VerificationCode` |
| **Application** | `IAuthService` / `AuthService`, `IProfileService` / `ProfileService`, DTO + FluentValidation, `IRepository<T>`, `IJwtTokenService`, `IPasswordHasher`, `IEmailSender` |
| **Infrastructure** | `EfRepository<T>`, `UserRepository`, `JwtTokenService`, `PasswordHasher` (PBKDF2), `EmailSender` (SMTP), DI extension |
| **API** | `AuthController`, `ProfileController`, `ExceptionHandlingMiddleware`, `ValidationFilter`, JWT Bearer config, Swagger + Bearer auth |

### 1.2 Cấu hình môi trường

`appsettings.json` (Production) **bắt buộc** có `Jwt:SecretKey` dài ≥ 32 ký tự. Khuyến nghị dùng User Secrets:

```bash
cd StudentFleaMarket.API
dotnet user-secrets init                       # đã có UserSecretsId trong csproj
dotnet user-secrets set "Jwt:SecretKey" "<chuỗi-ngẫu-nhiên-64-ký-tự>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=StudentFleaMarketDb;Username=postgres;Password=<mật-khẩu>"
```

Cấu hình email SMTP (mặc định `Email:LogOnly = true` → OTP được ghi ra log console):

```bash
dotnet user-secrets set "Email:LogOnly" "false"
dotnet user-secrets set "Email:Host" "smtp.gmail.com"
dotnet user-secrets set "Email:Port" "587"
dotnet user-secrets set "Email:UseSsl" "true"
dotnet user-secrets set "Email:Username" "<email-gửi>"
dotnet user-secrets set "Email:Password" "<app-password>"
```

### 1.3 Áp dụng migration

```bash
dotnet ef database update --project StudentFleaMarket.Infrastructure --startup-project StudentFleaMarket.API
```

Migration `AddAuthFeature` tạo bảng `RefreshTokens`, `VerificationCodes`, thêm cột `Users.CommonArea`, `Users.Dormitory` và giới hạn độ dài các cột profile.

> Không có PostgreSQL/Docker trên máy? Bật `"UseInMemoryDatabase": true` trong `appsettings.Development.json` để chạy thử nhanh (dữ liệu không persist).

### 1.4 Danh sách API

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| POST | `/api/Auth/register` | – | Đăng ký tài khoản (email thường hoặc email trường). Tự động gửi OTP. **Không trả token** — phải xác thực email rồi mới đăng nhập |
| POST | `/api/Auth/login` | – | Đăng nhập → Access Token + Refresh Token. Nhận `identifier` (username **hoặc** email) |
| POST | `/api/Auth/refresh` | – | Đổi token (rotation). Phát hiện dùng lại token cũ → thu hồi toàn bộ session |
| POST | `/api/Auth/logout` | – | Thu hồi refresh token |
| POST | `/api/Auth/send-verification-code` | – | Gửi OTP xác thực email (hạn 10 phút) |
| POST | `/api/Auth/resend-verification-code` | – | Gửi lại OTP |
| POST | `/api/Auth/verify-email` | – | Xác thực email bằng OTP 6 chữ số |
| GET | `/api/Profile` | ✅ | Lấy hồ sơ sinh viên |
| PUT | `/api/Profile` | ✅ | Cập nhật avatar, SĐT, trường, mã SV, khu vực, ký túc xá |
| POST | `/api/Profile/change-password` | ✅ | Đổi mật khẩu (thu hồi toàn bộ phiên đăng nhập) |
| POST | `/api/ServiceAccounts` | ✅ Admin | Tạo tài khoản dịch vụ AI → sinh API key (hiển thị 1 lần) |
| POST | `/api/ServiceAccounts/{id}/revoke` | ✅ Admin | Thu hồi API key của dịch vụ AI |

### 1.4.1 Vai trò & cách đăng nhập

Có **3 vai trò** (lưu trong cột `Users."Role"`):

| Role | Giá trị | Đăng nhập bằng | Ghi chú |
|------|---------|----------------|---------|
| **User** | `0` | Email (và username nếu đã đặt) | Đăng ký tự do qua `/api/Auth/register` |
| **Admin** | `1` | Email (và username nếu đã đặt) | Seed sẵn: `admin` / `admin123` — email `admin@sv.edu.vn` |
| **AiService** | `2` | **API key** (header `X-Api-Key`) | Dịch vụ AI: kiểm duyệt, gợi ý sản phẩm… Không đăng nhập bằng email/password |

**Username** là tùy chọn khi đăng ký:
- Có `username` → đăng nhập được bằng **cả username lẫn email** (`admin` hoặc `admin@sv.edu.vn`).
- Bỏ trống → tài khoản **chỉ** đăng nhập được bằng email.
- Quy tắc: 3–30 ký tự, chỉ gồm chữ / số / `.` / `_`, **duy nhất toàn hệ thống**.
- Lưu lowercase trong DB → đăng nhập không phân biệt hoa thường.

Tài khoản dịch vụ AI nằm ở bảng riêng `ServiceAccounts` — **không** có email/password, chỉ có API key (lưu dạng hash SHA-256).


### 1.5 Ví dụ request/response

**Đăng ký**

```json
POST /api/Auth/register
{
  "fullName": "Nguyễn Văn A",
  "email": "a.stu@sv.edu.vn",
  "password": "Abc12345",
  "confirmPassword": "Abc12345",
  "phoneNumber": "0901234567",
  "schoolName": "Đại học CNTT",
  "studentCode": "SV001",
  "username": "nguyenvana"
}
```

```json
{
  "message": "Đăng ký thành công. Vui lòng kiểm tra email để xác thực tài khoản trước khi đăng nhập.",
  "user": {
    "id": "8e653a10-1bf9-4b88-8e9f-49c492915a0c",
    "fullName": "Nguyễn Văn A",
    "email": "a.stu@sv.edu.vn",
    "username": "nguyenvana",
    "phoneNumber": "0901234567",
    "avatarUrl": null,
    "schoolName": "Đại học CNTT",
    "studentCode": "SV001",
    "commonArea": null,
    "dormitory": null,
    "role": 0,
    "roleName": "User",
    "status": 1,
    "isEmailVerified": false,
    "createdAt": "2026-10-23T07:47:59Z",
    "updatedAt": "2026-10-23T07:47:59Z"
  },
  "verificationEmail": "a.stu@sv.edu.vn"
}
```

**Đăng nhập** (username hoặc email)

```json
POST /api/Auth/login
{
  "identifier": "admin",
  "password": "admin123"
}
```

**Tạo tài khoản dịch vụ AI** (chỉ Admin)

```json
POST /api/ServiceAccounts
Authorization: Bearer <adminAccessToken>

{
  "name": "ai-moderation"
}
```

```json
{
  "id": "83a1a3c3-19b1-44e6-b28c-885f8f67e6a3",
  "name": "ai-moderation",
  "apiKey": "sfm_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "keyPrefix": "sfm_xxxxxxxx",
  "roleName": "AiService",
  "expiresAt": null,
  "createdAt": "2026-10-23T09:30:24Z"
}
```

> API key chỉ hiển thị **đúng 1 lần** — DB chỉ lưu hash SHA-256.
> Dịch vụ AI gọi API bằng header `X-Api-Key: sfm_...`.

**Cập nhật hồ sơ**

```json
PUT /api/Profile
Authorization: Bearer <accessToken>

{
  "fullName": "Nguyễn Văn A",
  "phoneNumber": "0901234567",
  "avatarUrl": "https://cdn.example.com/avatars/abc.png",
  "schoolName": "Đại học CNTT",
  "studentCode": "SV001",
  "commonArea": "Khu A - Bắc",
  "dormitory": "KTX A - Tầng 3, Phòng 302"
}
```

### 1.6 Quy ước response lỗi

```json
{
  "success": false,
  "message": "Email hoặc mật khẩu không đúng.",
  "code": "INVALID_CREDENTIALS"
}
```

| HTTP | Code | Ý nghĩa |
|------|------|---------|
| 400 | `VALIDATION_ERROR` | Dữ liệu không hợp lệ (kèm mảng `errors` chi tiết) |
| 400 | `INVALID_CODE` / `CODE_EXPIRED` / `CODE_ALREADY_USED` | Lỗi OTP |
| 401 | `INVALID_CREDENTIALS` | Sai email / username / mật khẩu |
| 401 | `INVALID_API_KEY` | API key của dịch vụ AI không hợp lệ / hết hạn / đã thu hồi |
| 401 | `REFRESH_TOKEN_REVOKED` | Refresh token đã bị thu hồi / phát hiện dùng lại |
| 401 | `ACCOUNT_BANNED` / `ACCOUNT_INACTIVE` | Tài khoản bị khóa |
| 403 | `EMAIL_NOT_VERIFIED` | Chưa xác thực email — cần verify OTP trước khi đăng nhập |
| 403 | `SERVICE_ACCOUNT_REQUIRES_API_KEY` | Tài khoản dịch vụ AI phải xác thực bằng API key |
| 409 | `EMAIL_EXISTS` | Email đã đăng ký |
| 409 | `USERNAME_EXISTS` | Tên tài khoản đã tồn tại |
| 409 | `SERVICE_NAME_EXISTS` | Tên dịch vụ AI đã tồn tại |

### 1.7 Ghi chú bảo mật

- Mật khẩu băm bằng **PBKDF2** (ASP.NET Core Identity), không lưu plaintext.
- Refresh token, OTP & API key chỉ lưu **SHA-256 hash** trong DB — lộ DB cũng không lấy được token dùng được.
- Access token hạn 30 phút, refresh token 7 ngày, có **rotation** + phát hiện **token reuse** (thu hồi toàn bộ session).
- OTP hạn 10 phút, tối đa 5 lần thử sai, mã cũ tự động bị vô hiệu khi gửi mã mới.
- Đổi mật khẩu → thu hồi toàn bộ refresh token của user.
- Response đăng nhập không lộ email nào tồn tại (thông báo chung).
- **Đăng ký không cấp token**; **đăng nhập bị chặn (403 `EMAIL_NOT_VERIFIED`)** cho tới khi xác thực email qua OTP.
- **Tài khoản dịch vụ AI** nằm ở bảng riêng `ServiceAccounts`, không có email/password; xác thực bằng header `X-Api-Key`; key có thể thu hồi và tự hết hạn.
- Swagger UI: http://localhost:&lt;port&gt;/swagger (Development) — nhấn **Authorize** để nhập token.

### 1.8 Tài khoản mặc định (seed)

| Username | Email | Mật khẩu | Vai trò |
|----------|-------|----------|---------|
| `admin` | `admin@sv.edu.vn` | `admin123` | Admin (đã xác thực email) |

> ⚠️ **Đổi mật khẩu `admin123` sau lần đăng nhập đầu tiên.**
> Seed chạy tự động khi khởi động API và là **idempotent** — không ghi đè dữ liệu đã có.
> Bỏ qua seed: đặt `"SkipDatabaseSeed": true` trong `appsettings.Development.json`.
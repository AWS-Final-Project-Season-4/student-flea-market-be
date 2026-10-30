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

Migration `AddUsernameAndServiceAccounts` thêm cột `Users.Username` và bảng `ServiceAccounts`.

Migration `AddItemSearchIndexes` thêm index hỗ trợ tìm kiếm & lọc nâng cao trên bảng `Items`: `Title`, `CategoryId`, `Price`, `Condition`, `Status`, `SellerId`, `CreatedAt`.

Migration `AddItemTitleNormalized` thêm cột `Items.TitleNormalized` (bản bỏ dấu tiếng Việt của `Title`) + index. Cột này cho phép tìm kiếm **không phân biệt dấu**: `?keyword=sach` khớp "Sách", `?keyword=lap trinh` khớp "Lập trình".

Migration `AddSellerApproval` thêm các cột quản lý quyền bán trên bảng `Users`: `SellerStatus`, `SellerRequestedAt`, `SellerApprovedAt`, `SellerApprovedByUserId`, `SellerRejectionReason`. User mới đăng ký có `SellerStatus = 0` (chưa được cấp quyền bán).

Migration `AddAdminBanUser` thêm các cột quản lý cấm tài khoản trên bảng `Users`: `BanReason`, `BannedByUserId`, `BannedAt`, `UnbannedAt`.

Migration `AddAdminRoleGrant` thêm các cột theo dõi việc cấp quyền quản trị trên bảng `Users`: `RoleGrantedByUserId`, `RoleGrantedAt`, `RoleRevokedAt`.

> Migration `RemoveAdminRoleGrant` **xoá lại** 3 cột trên (vì dự án đã bỏ tính năng cấp / gỡ quyền quản trị).

> Không có PostgreSQL/Docker trên máy? Bật `"UseInMemoryDatabase": true` trong `appsettings.Development.json` để chạy thử nhanh (dữ liệu không persist).

### 1.3.1 Dữ liệu demo (seed mẫu)

Khi chạy ở môi trường **Development**, API tự động nạp dữ liệu mẫu để test nhanh các chức năng tìm kiếm / đăng bán / gỡ bán.

**Tài khoản demo** (mật khẩu chung: `123456`, email đã xác thực sẵn — không cần OTP):

| Username | Email | Họ tên | Vai trò |
|----------|-------|--------|---------|
| `minh` | `minh@sv.edu.vn` | Nguyễn Minh | Sinh viên — chủ nhiều tin nhất |
| `linh` | `linh@sv.edu.vn` | Trần Linh | Sinh viên |
| `tu` | `tu@sv.edu.vn` | Lê Tu | Sinh viên |
| `admin` | `admin@sv.edu.vn` | Quản trị viên | Admin — mật khẩu `admin123` |

**Dữ liệu mẫu**: 25 tin đăng trải đều 9 danh mục, ưu tiên các tình huống để test:
- Nhiều mức giá (0đ tặng → 5.000.000đ) và độ mới khác nhau
- Có tin cho phép đổi đồ (`exchangeOnly=true`)
- Có tin ở nhiều địa điểm khác nhau (`KTX Khu A/B/C`, `Thư viện`, `Sân vận động`…)
- Có tin ở các trạng thái: **đang bán**, **đã đặt**, **đã bán**, **đã gỡ** (soft delete) → test lọc `statuses`
- 10 lượt yêu thích → test `sortBy=most_favorited`

**Cấu hình** (trong `appsettings.Development.json`):

| Key | Mặc định | Ý nghĩa |
|-----|----------|----------|
| `SeedDemoData` | `true` (Development) | Bật/tắt dữ liệu demo. Bắt buộc `true` nếu muốn dùng ở Production |
| `SkipDemoDataSeed` | `false` | Đặt `true` để tắt demo ở Development |
| `SkipDatabaseSeed` | `false` | Tắt toàn bộ seed (kể cả admin + danh mục) |

> ✅ **Idempotent** — chạy lại bao nhiêu lần cũng không nhân bản dữ liệu (đã kiểm chứng: restart 2 lần, số tin vẫn là 25, `sellerId` không đổi).


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
| GET | `/api/Profile` | ✅ User/Admin | Lấy hồ sơ **của chính mình** (lấy `userId` từ token) |
| PUT | `/api/Profile` | ✅ User/Admin | Cập nhật avatar, SĐT, trường, mã SV, khu vực, ký túc xá |
| POST | `/api/Profile/change-password` | ✅ User/Admin | Đổi mật khẩu (thu hồi toàn bộ phiên đăng nhập) |
| POST | `/api/ServiceAccounts` | ✅ Admin | Tạo tài khoản dịch vụ AI → sinh API key (hiển thị 1 lần) |
| POST | `/api/ServiceAccounts/{id}/revoke` | ✅ Admin | Thu hồi API key của dịch vụ AI |

**Tin đăng & Danh mục (Item & Catalog)**

> 📂 **Tách 2 nhóm endpoint rõ ràng:**
> - `api/Items/search` + `api/Items/...` → **TÌM KIẾM & XEM TIN** (public, không cần token)
> - `api/Items/sell/...` → **ĐĂNG BÁN & QUẢN LÝ TIN CỦA MÌNH** (cần token)

**Nhóm 1 — Tìm kiếm & xem tin (không cần token)**

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| GET | `/api/Items/search` | – | Tìm kiếm & lọc nâng cao (keyword, danh mục, giá, độ mới, trạng thái, đổi đồ, địa điểm, người bán, sắp xếp, phân trang) |
| GET | `/api/Items/categories` | – | Danh sách danh mục kèm số lượng tin đang bán |
| GET | `/api/Items/{id}` | – | Chi tiết tin đăng (kèm ảnh, người bán, số lượt yêu thích) |
| GET | `/api/Items/slug/{slug}` | – | Chi tiết tin đăng theo slug (URL thân thiện) |
| GET | `/api/Items/seller/{sellerId}` | – | Tin đăng của một người bán |

**Nhóm 2 — Đăng bán & quản lý tin của mình (cần token)**

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| POST | `/api/Items/sell` | ✅ User/Admin | Đăng tin bán đồ cũ. Giá `0` = tặng miễn phí. **Cần quyền bán** (`SellerStatus = Approved`) |
| PUT | `/api/Items/sell/{id}` | ✅ User/Admin | Cập nhật tin đăng (chỉ chủ tin). **Cần quyền bán** (`SellerStatus = Approved`) |
| DELETE | `/api/Items/sell/{id}` | ✅ User/Admin | Gỡ tin đăng (soft delete — chủ tin hoặc Admin) |
| GET | `/api/Items/sell/mine` | ✅ User/Admin | Tin đăng của chính mình (gồm cả tin đã gỡ / đã bán) |

**Nhóm 3 — Yêu thích (Wishlist)**

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| GET | `/api/Favorites` | ✅ User/Admin | Danh sách yêu thích (Wishlist) của chính mình |
| POST | `/api/Favorites/{itemId}` | ✅ User/Admin | Thêm vào yêu thích (idempotent) |
| DELETE | `/api/Favorites/{itemId}` | ✅ User/Admin | Xóa khỏi yêu thích |
| POST | `/api/Favorites/{itemId}/toggle` | ✅ User/Admin | Đảo trạng thái yêu thích (nút "heart" một chạm) |

**Nhóm 4 — Quyền bán hàng (Seller) — phía người dùng**

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| GET | `/api/Seller/status` | ✅ User/Admin | Trạng thái quyền bán của chính mình (`canSell` = đã được phép đăng tin chưa) |
| POST | `/api/Seller/request` | ✅ User/Admin | Gửi yêu cầu trở thành người bán (chờ Admin duyệt). Có thể gửi lại nếu trước đó bị từ chối |
| POST | `/api/Seller/request/cancel` | ✅ User/Admin | Hủy yêu cầu đang chờ duyệt |

**Nhóm 5 — Admin duyệt người bán**

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| GET | `/api/Admin/seller-requests` | ✅ Admin | Danh sách yêu cầu trở thành người bán. Lọc `?status=1` (chờ duyệt) / `2` (đã duyệt) / `3` (bị từ chối) |
| POST | `/api/Admin/seller-requests/{userId}/approve` | ✅ Admin | Cấp quyền đăng bán cho tài khoản |
| POST | `/api/Admin/seller-requests/{userId}/reject` | ✅ Admin | Từ chối yêu cầu (body: `{ "reason": "..." }`) |
| POST | `/api/Admin/seller-requests/{userId}/revoke` | ✅ Admin | Thu hồi quyền bán đã cấp |

**Nhóm 6 — Admin quản lý & cấm tài khoản**

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| GET | `/api/Admin/users` | ✅ Admin | Danh sách tài khoản. Lọc `?status=1` (Hoạt động) / `2` (Chưa kích hoạt) / `3` (Đã bị cấm), tìm `?keyword=` (họ tên/email/username) |
| GET | `/api/Admin/users/{userId}` | ✅ Admin | Chi tiết tài khoản |
| POST | `/api/Admin/users/{userId}/ban` | ✅ Admin | **Cấm tài khoản** (body: `{ "reason": "..." }`) |
| POST | `/api/Admin/users/{userId}/unban` | ✅ Admin | **Gỡ cấm** — tài khoản hoạt động trở lại |

> ℹ️ Dự án **không có** API cấp / gỡ quyền quản trị. Vai trò Admin được gán trực tiếp trong DB
> (tài khoản `admin` mặc định được seed khi khởi tạo) → không có đường leo thang quyền qua API.

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

#### 🛒 Seller — "người bán" là NĂNG LỰC, không phải vai trò riêng

`Role` chỉ có 3 giá trị ở trên. Việc "được phép đăng bán" được quản lý **riêng** bằng cột `Users."SellerStatus"`:

| SellerStatus | Giá trị | Ý nghĩa | Có đăng bán được? |
|--------------|----------|---------|-------------------|
| **None** | `0` | Chưa đăng ký bán (mặc định khi đăng ký tài khoản) | ❌ `403 SELLER_NOT_APPROVED` |
| **Pending** | `1` | Đã gửi yêu cầu, đang chờ Admin duyệt | ❌ `403 SELLER_REQUEST_PENDING` |
| **Approved** | `2` | Được Admin cấp quyền | ✅ |
| **Rejected** | `3` | Bị Admin từ chối (kèm lý do) | ❌ `403 SELLER_REQUEST_REJECTED` |

**Tại sao tách khỏi `Role`?**
- Người bán **vẫn là `User`** → vẫn mua, yêu thích, xem hồ sơ, bình thường. Chỉ chức năng **đăng tin bán** bị khoá.
- Không phải sửa lại JWT hay `[Authorize(Roles = ...)]` trên toàn bộ controller.
- `UserStatus` (Active/Inactive/Banned) là **trạng thái tài khoản** — tách riêng, không trộn với `SellerStatus`.

**Ai cần duyệt?** Chỉ **Admin** mới cấp được quyền bán. Admin không cần xin — đã có sẵn toàn bộ quyền.

**Luật "được phép đăng bán" — nguồn sự thật duy nhất:** `SellerPermission` (Application/Common/Helpers/SellerPermission.cs).
- `CanSell(user)` → `true` khi user là **Admin** hoặc `SellerStatus = Approved`.
- `EnsureCanSell(user)` → ném **403** với mã lỗi phù hợp từng trạng thái nếu không được phép.
- Áp dụng cho **cả** `POST /api/Items/sell` (tạo tin) **và** `PUT /api/Items/sell/{id}` (sửa tin) — seller bị mất quyền bán không sửa được tin cũ.

**Trạng thái trả về** trong mọi response `user` (login / register / profile):

```json
{
  "role": 0,
  "roleName": "User",
  "sellerStatus": 2,
  "sellerStatusName": "Đã được duyệt",
  "canSell": true
}
```

**Cột bổ sung trong DB** (migration `AddSellerApproval`):
`Users.SellerStatus`, `Users.SellerRequestedAt`, `Users.SellerApprovedAt`, `Users.SellerApprovedByUserId`, `Users.SellerRejectionReason`.

#### 🚫 Admin cấm tài khoản (ban)

Trạng thái tài khoản nằm ở cột `Users."Status"` — **tách riêng** khỏi cả `Role` lẫn `SellerStatus`:

| UserStatus | Giá trị | Ý nghĩa |
|------------|---------|----------|
| **Active** | `1` | Hoạt động bình thường (mặc định khi đăng ký) |
| **Inactive** | `2` | Chưa kích hoạt (email chưa xác thực) |
| **Banned** | `3` | **Bị Admin cấm** |

**Khi Admin cấm một user, 4 việc xảy ra:**

1. `Status = Banned` → đăng nhập bị chặn **401 `ACCOUNT_BANNED`** (kèm lý do trong message).
2. **Toàn bộ refresh token bị thu hồi** → user không lấy được access token mới, buộc phải đăng nhập lại.
3. **Quyền bán bị thu hồi** (nếu đang có) → tài khoản bị khoá không đăng tin mới được.
4. Gửi email thông báo cho user.

**Access token cũ vẫn còn hiệu lực — nhưng bị chặn ngay.**
JWT là *stateless*, server không tra DB mỗi request, nên access token đã cấp vẫn dùng được tới khi hết hạn (30 phút). Vì vậy middleware `BannedUserBlockMiddleware` chạy sau `UseAuthentication` và tra trạng thái tài khoản mỗi request (chỉ `SELECT Status, "BanReason"` theo PK) → trả **403 `ACCOUNT_BANNED`** nếu tài khoản đã bị cấm.

> ⚠️ **Admin không thể cấm/gỡ cấm tài khoản Admin** → `400 ADMIN_CANNOT_BE_BANNED` (tránh khoá nhầm hệ thống).

**Cột bổ sung trong DB** (migration `AddAdminBanUser`):
`Users.BanReason`, `Users.BannedByUserId`, `Users.BannedAt`, `Users.UnbannedAt`.

#### 👑 Vai trò quản trị (Role = Admin)

Dự án **không có API cấp / gỡ quyền quản trị** — vai trò Admin được gán **trực tiếp trong DB**.

| Cách | Mô tả |
|------|-------|
| Tài khoản `admin` mặc định | Được `DatabaseSeeder` seed khi khởi tạo (`admin` / `admin123` — nên đổi mật khẩu sau lần đăng nhập đầu) |
| Gán trực tiếp trong DB | `UPDATE "Users" SET "Role" = 1 WHERE "Email" = '...'` |

> 🔒 **Lý do bỏ tính năng này:** không có đường leo thang quyền — Admin nào cũng không thể tự cấp quyền admin cho người khác, và không ai tự thu hồi được quyền admin của người khác.
>
> Cơ chế `RoleSyncMiddleware` **vẫn giữ nguyên** (tra `Role` từ DB mỗi request, chỉ `SELECT "Role"` theo PK → rất nhẹ) → nếu role được đổi trực tiếp trong DB thì hiệu lực **tức thì**, không cần đăng nhập lại.

#### 🗑 Mất quyền bán → gỡ toàn bộ tin đăng

Khi Admin **thu hồi quyền bán** (`POST /api/Admin/seller-requests/{userId}/revoke`), **từ chối yêu cầu** (`POST /api/Admin/seller-requests/{userId}/reject`) **hoặc cấm tài khoản** (`POST /api/Admin/users/{userId}/ban`):

> **Toàn bộ tin đăng của người bán đó sẽ bị gỡ khỏi sàn** (soft delete hàng loạt: `IsDeleted = true`, `Status = Removed`).

Lý do: nếu không, tin vẫn hiển thị công khai dù người bán không còn quyền đăng → vi phạm kỳ vọng của người mua.

- Dùng **soft delete** (không xóa vật lý) → giữ lịch sử giao dịch & lượt yêu thích.
- Tin đã gỡ **vẫn hiện** trong `/api/Items/sell/mine` của chủ tin (với trạng thái "Đã gỡ").
- User vẫn có thể gửi yêu cầu trở thành người bán lại.

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
    "updatedAt": "2026-10-23T07:47:59Z",
    "sellerStatus": 0,
    "sellerStatusName": "Chưa đăng ký bán",
    "canSell": false
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

**Luồng xin quyền bán hàng (Seller)**

```json
// 1. User xem trạng thái hiện tại
GET /api/Seller/status
Authorization: Bearer <userAccessToken>

{
  "sellerStatus": 0,
  "sellerStatusName": "Chưa đăng ký bán",
  "canSell": false,
  "requestedAt": null,
  "approvedAt": null,
  "rejectionReason": null
}
```

```json
// 2. User gửi yêu cầu trở thành người bán
POST /api/Seller/request
Authorization: Bearer <userAccessToken>

{
  "note": "Tôi muốn bán đồ cũ, đã xác thực sinh viên."
}

{
  "sellerStatus": 1,
  "sellerStatusName": "Đang chờ duyệt",
  "canSell": false,
  "requestedAt": "2026-10-28T03:14:12Z",
  "approvedAt": null,
  "rejectionReason": null
}
```

```json
// 3. Admin xem danh sách chờ duyệt
GET /api/Admin/seller-requests?status=1
Authorization: Bearer <adminAccessToken>

{
  "items": [
    {
      "userId": "8e653a10-1bf9-4b88-8e9f-49c492915a0c",
      "fullName": "Nguyễn Văn A",
      "email": "a.stu@sv.edu.vn",
      "username": "nguyenvana",
      "schoolName": "Đại học CNTT",
      "studentCode": "SV001",
      "phoneNumber": "0901234567",
      "sellerStatus": 1,
      "sellerStatusName": "Đang chờ duyệt",
      "requestedAt": "2026-10-28T03:14:12Z",
      "approvedAt": null,
      "rejectionReason": null,
      "itemCount": 0
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1,
  "hasPrevious": false,
  "hasNext": false
}
```

```json
// 4. Admin cấp quyền bán  (hoặc /reject với body { "reason": "..." })
POST /api/Admin/seller-requests/8e653a10-1bf9-4b88-8e9f-49c492915a0c/approve
Authorization: Bearer <adminAccessToken>

{
  "userId": "8e653a10-1bf9-4b88-8e9f-49c492915a0c",
  "fullName": "Nguyễn Văn A",
  "email": "a.stu@sv.edu.vn",
  "sellerStatus": 2,
  "sellerStatusName": "Đã được duyệt",
  "requestedAt": "2026-10-28T03:14:12Z",
  "approvedAt": "2026-10-28T03:14:50Z",
  "rejectionReason": null,
  "itemCount": 0
}
```

> Từ bước 4 trở đi, user đã có thể gọi `POST /api/Items/sell` bình thường.

**Admin cấm / gỡ cấm tài khoản**

```json
// 1. Admin xem danh sách tài khoản (lọc + tìm kiếm)
GET /api/Admin/users?status=3&keyword=minh
Authorization: Bearer <adminAccessToken>

{
  "items": [
    {
      "id": "8e653a10-1bf9-4b88-8e9f-49c492915a0c",
      "fullName": "Nguyễn Minh",
      "email": "minh@sv.edu.vn",
      "username": "minh",
      "schoolName": "Đại học Bách Khoa",
      "studentCode": "BK22DS001",
      "status": 3,
      "statusName": "Đã bị cấm",
      "isBanned": true,
      "banReason": "Đăng tin đồ giả",
      "bannedAt": "2026-10-28T03:45:00Z",
      "unbannedAt": null,
      "sellerStatus": 0,
      "sellerStatusName": "Chưa đăng ký bán",
      "canSell": false,
      "isEmailVerified": true,
      "itemCount": 10
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1,
  "hasPrevious": false,
  "hasNext": false
}
```

```json
// 2. Admin cấm tài khoản
POST /api/Admin/users/8e653a10-1bf9-4b88-8e9f-49c492915a0c/ban
Authorization: Bearer <adminAccessToken>

{
  "reason": "Đăng tin đồ giả"
}
```

```json
// 3. Admin gỡ cấm
POST /api/Admin/users/8e653a10-1bf9-4b88-8e9f-49c492915a0c/unban
Authorization: Bearer <adminAccessToken>
```

> ℹ️ Dự án không có endpoint cấp / gỡ quyền quản trị. Muốn thêm Admin mới thì gán role trực tiếp trong DB:
> `UPDATE "Users" SET "Role" = 1 WHERE "Email" = 'email-cua-ban@example.com'`

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
| 401 | `UNAUTHORIZED` | Thiếu token, token không hợp lệ, hoặc chưa đăng nhập |
| 401 | `TOKEN_EXPIRED` | Access token đã hết hạn — cần đăng nhập lại |
| 401 | `INVALID_TOKEN_SIGNATURE` / `INVALID_TOKEN_AUDIENCE` / `INVALID_TOKEN_ISSUER` | Token sai chữ ký / sai issuer / sai audience |
| 401 | `ACCOUNT_BANNED` / `ACCOUNT_INACTIVE` | Tài khoản bị khóa |
| 403 | `FORBIDDEN` | Đã đăng nhập nhưng vai trò không được phép (ví dụ tài khoản AI gọi `/api/Profile`) |
| 403 | `EMAIL_NOT_VERIFIED` | Chưa xác thực email — cần verify OTP trước khi đăng nhập |
| 403 | `SERVICE_ACCOUNT_REQUIRES_API_KEY` | Tài khoản dịch vụ AI phải xác thực bằng API key |
| 409 | `EMAIL_EXISTS` | Email đã đăng ký |
| 409 | `USERNAME_EXISTS` | Tên tài khoản đã tồn tại |
| 409 | `SERVICE_NAME_EXISTS` | Tên dịch vụ AI đã tồn tại |
| 404 | `ITEM_NOT_FOUND` / `CATEGORY_NOT_FOUND` | Không tìm thấy tin đăng / danh mục |
| 403 | `NOT_ITEM_OWNER` | Không phải chủ tin → không được sửa hoặc xóa tin đăng này |
| 400 | `INVALID_PRICE_RANGE` | Giá tối thiểu lớn hơn giá tối đa |
| 400 | `INVALID_SORT_BY` | Tiêu chí sắp xếp không hợp lệ |
| 400 | `ITEM_ALREADY_SOLD` / `ITEM_NOT_AVAILABLE` | Tin đã bán / không còn khả dụng |
| 403 | `AI_CANNOT_CREATE_ITEM` | Tài khoản dịch vụ AI không được đăng tin bán |
| 403 | `SELLER_NOT_APPROVED` | Chưa được cấp quyền bán — cần gửi yêu cầu và chờ Admin duyệt |
| 403 | `SELLER_REQUEST_PENDING` | Yêu cầu trở thành người bán đang chờ Admin duyệt |
| 403 | `SELLER_REQUEST_REJECTED` | Yêu cầu trở thành người bán đã bị từ chối — có thể gửi lại |
| 409 | `SELLER_REQUEST_PENDING` | Đã có yêu cầu đang chờ duyệt (khi gửi yêu cầu mới) |
| 409 | `SELLER_ALREADY_APPROVED` | Tài khoản đã được cấp quyền bán |
| 409 | `SELLER_ALREADY_REJECTED` | Yêu cầu này đã bị từ chối trước đó |
| 400 | `ADMIN_ALREADY_CAN_SELL` | Tài khoản Admin không cần xin quyền bán |
| 400 | `ADMIN_CANNOT_REJECT` / `ADMIN_CANNOT_REVOKE` | Không thể từ chối / thu hồi quyền bán của Admin |
| 400 | `SELLER_USE_REVOKE_INSTEAD` | Tài khoản đang có quyền bán — hãy dùng thao tác **thu hồi** thay vì **từ chối** |
| 400 | `BANNED_USER_CANNOT_BE_SELLER` | Không thể cấp quyền bán cho tài khoản đang bị cấm |
| 400 | `SELLER_NO_PENDING_REQUEST` | Không có yêu cầu nào đang chờ duyệt để hủy |
| 403 | `ACCOUNT_BANNED` | Tài khoản đã bị Admin cấm (middleware chặn cả access token cũ) |
| 401 | `ACCOUNT_BANNED` | Đăng nhập bị chặn vì tài khoản đã bị cấm (message kèm lý do) |
| 400 | `ADMIN_CANNOT_BE_BANNED` | Không thể cấm / gỡ cấm tài khoản Admin |
| 400 | `USER_NOT_BANNED` | Tài khoản hiện không bị cấm (khi gỡ cấm) |
| 409 | `USER_ALREADY_BANNED` | Tài khoản đã bị cấm trước đó |

### 1.6.1 Tin đăng & Danh mục (Feature 2)

**Đăng tin bán**

```json
POST /api/Items/sell
{
  "title": "Sách Lập trình C# cơ bản",
  "categoryId": 1,
  "description": "Sách mới 90%, không ghi chú",
  "price": 50000,
  "condition": 2,
  "isExchangeEnabled": true,
  "location": "KTX Khu A",
  "imageUrls": ["https://cdn.example.com/items/abc-1.jpg"]
}
```

| Trường | Bắt buộc | Quy tắc |
|---------|-----------|---------|
| `title` | ✅ | 3–200 ký tự |
| `categoryId` | ✅ | Phải tồn tại trong `/api/Items/categories` |
| `description` | ❌ | Tối đa 5000 ký tự |
| `price` | ✅ | `0` = tặng miễn phí · không âm · ≤ 100.000.000 |
| `condition` | ✅ | `1`=Mới 99% · `2`=Như mới · `3`=Tốt · `4`=Khá · `5`=Kém |
| `isExchangeEnabled` | ❌ | Cho phép đổi đồ (mặc định `false`) |
| `location` | ✅ | 2–255 ký tự — địa điểm gặp mặt trực tiếp |
| `imageUrls` | ❌ | Tối đa 8 URL · ảnh đầu tiên là ảnh chính |

**Tìm kiếm & lọc nâng cao** — `GET /api/Items/search`

| Query param | Kiểu | Mô tả |
|-------------|------|-------|
| `keyword` | string | Tìm trong tiêu đề — **không phân biệt dấu tiếng Việt & hoa thường** (`sach` khớp "Sách", `lap trinh` khớp "Lập trình") |
| `categoryIds` | int[] | Lọc nhiều danh mục (`?categoryIds=1&categoryIds=5`) |
| `categorySlug` | string | Lọc theo slug danh mục (`sach`, `dien-tu`…) |
| `minPrice` / `maxPrice` | decimal | Khoảng giá |
| `conditions` | int[] | Lọc theo độ mới (`?conditions=1&conditions=2`) |
| `statuses` | int[] | Lọc theo trạng thái — mặc định chỉ `1` (Đang bán) |
| `exchangeOnly` | bool | Chỉ hiện tin cho phép đổi đồ |
| `location` | string | Lọc theo địa điểm (chứa) |
| `sellerId` | guid | Lọc theo người bán |
| `sortBy` | string | `newest` (mặc định) · `oldest` · `price_asc` · `price_desc` · `most_favorited` |
| `page` / `pageSize` | int | Phân trang — `pageSize` tối đa 100, mặc định 20 |

Response kèm metadata phân trang:

```json
{
  "items": [ /* ... */ ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3,
  "hasPrevious": false,
  "hasNext": true
}
```

> 💡 **Phân quyền tin đăng**
> - Chỉ **chủ tin** được sửa / gỡ tin của mình → người khác nhận **403 `NOT_ITEM_OWNER`**.
> - Tài khoản dịch vụ AI (`AiService`, xác thực bằng API key) **không được đăng tin** → **403 `AI_CANNOT_CREATE_ITEM`**.
> - Tài khoản **chưa được Admin cấp quyền bán** → **403 `SELLER_NOT_APPROVED`** (xem mục 1.4.1).
> - Gỡ tin là **soft delete** (`IsDeleted = true`) → tin vẫn nằm trong DB, chỉ ẩn khỏi truy vấn công khai.
> - `/api/Items/sell/mine` của chủ tin **vẫn thấy** tin đã gỡ / đã bán (bỏ qua global query filter).
> - Yêu thích là **duy nhất theo cặp `(userId, itemId)`** → thêm lại nhiều lần không tạo bản ghi trùng.

### 1.6.2 Test nhanh với dữ liệu demo

Sau khi chạy `dotnet run` ở Development, dữ liệu mẫu đã sẵn sàng. Chuỗi lệnh kiểm tra nhanh:

```bash
# 1. Tìm kiếm (không cần token) — tìm không dấu
curl "http://localhost:5033/api/Items/search?keyword=sach"
curl "http://localhost:5033/api/Items/search?categorySlug=dien-tu&minPrice=100000"
curl "http://localhost:5033/api/Items/search?sortBy=most_favorited&exchangeOnly=true"

# 2. Đăng nhập demo user
curl -X POST http://localhost:5033/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"minh@sv.edu.vn","password":"123456"}'

# 3. Đăng bán tin
curl -X POST http://localhost:5033/api/Items/sell \
  -H "Authorization: Bearer <accessToken>" \
  -H "Content-Type: application/json" \
  -d '{"title":"May pha ca phe espresso","categoryId":9,"description":"May moi 95%","price":3500000,"condition":2,"isExchangeEnabled":true,"location":"KTX Khu A"}'

# 4. Xem tin của mình (gồm cả tin đã gỡ / đã bán)
curl http://localhost:5033/api/Items/sell/mine -H "Authorization: Bearer <accessToken>"

# 5. Gỡ bán
curl -X DELETE http://localhost:5033/api/Items/sell/<itemId> -H "Authorization: Bearer <accessToken>"
```

**Luồng duyệt người bán (Seller) — test từ đầu:**

```bash
# 6. Admin thu hồi quyền bán của user minh -> về trạng thái "chưa được cấp quyền"
curl -X POST http://localhost:5033/api/Admin/seller-requests/<minhUserId>/revoke \
  -H "Authorization: Bearer <adminAccessToken>"

# 7. User đăng bán -> bị chặn 403 SELLER_NOT_APPROVED
curl -X POST http://localhost:5033/api/Items/sell \
  -H "Authorization: Bearer <minhAccessToken>" \
  -H "Content-Type: application/json" \
  -d '{"title":"May pha ca phe espresso","categoryId":9,"price":3500000,"condition":2,"location":"KTX Khu A"}'

# 8. User gửi yêu cầu trở thành người bán
curl -X POST http://localhost:5033/api/Seller/request \
  -H "Authorization: Bearer <minhAccessToken>" \
  -H "Content-Type: application/json" \
  -d '{"note":"Toi muon ban do cu"}'

# 9. Admin xem danh sách chờ duyệt
curl "http://localhost:5033/api/Admin/seller-requests?status=1" \
  -H "Authorization: Bearer <adminAccessToken>"

# 10. Admin duyệt -> user đăng bán được
curl -X POST http://localhost:5033/api/Admin/seller-requests/<minhUserId>/approve \
  -H "Authorization: Bearer <adminAccessToken>"
```

**Kết quả mong đợi:**

| Bước | Kết quả |
|------|---------|
| Tìm `keyword=sach` | 4 tin (Sách Lập trình C#, Sách Lập trình Python, Cho xin sách cũ, Cho xin túi đựng sách) |
| Tìm `categorySlug=dien-tu` | 5 tin (bàn phím, chuột, tai nghe, sạc dự phòng, ổ cứng SSD) |
| `GET /api/Items/sell/mine` của `minh` | 10 tin (8 đang bán + 1 đã gỡ + 1 đã bán) |
| `POST /api/Items/sell` | `201` + slug tự sinh (`may-pha-ca-phe-espresso-xxxxxx`) |
| `DELETE /api/Items/sell/{id}` | Tin biến mất khỏi tìm kiếm công khai, vẫn thấy trong `/sell/mine` với trạng thái "Đã gỡ" |
| `DELETE` tin của người khác | `403 NOT_ITEM_OWNER` |
| Bước 6 → `POST /api/Items/sell` | `403 SELLER_NOT_APPROVED` |
| Bước 8 | `200` + `sellerStatus = 1` (Đang chờ duyệt), `canSell = false` |
| Bước 9 | Danh sách chờ duyệt có 1 yêu cầu (Nguyễn Minh) |
| Bước 10 | `sellerStatus = 2` (Đã được duyệt) → `POST /api/Items/sell` trả `201` |
| `GET /api/Favorites` của user bị từ chối | Vẫn `200` — seller vẫn mua/yêu thích bình thường |

> ⚠️ Demo user (`minh`, `linh`, `tu`) được seed với `SellerStatus = Approved` sẵn để luồng đăng bán không bị chặn. Muốn test luồng duyệt thì dùng bước 6 để thu hồi quyền trước.


### 1.7 Ghi chú bảo mật

- Mật khẩu băm bằng **PBKDF2** (ASP.NET Core Identity), không lưu plaintext.
- Refresh token, OTP & API key chỉ lưu **SHA-256 hash** trong DB — lộ DB cũng không lấy được token dùng được.
- Access token hạn 30 phút, refresh token 7 ngày, có **rotation** + phát hiện **token reuse** (thu hồi toàn bộ session).
- OTP hạn 10 phút, tối đa 5 lần thử sai, mã cũ tự động bị vô hiệu khi gửi mã mới.
- Đổi mật khẩu → thu hồi toàn bộ refresh token của user.
- Response đăng nhập không lộ email nào tồn tại (thông báo chung).
- **Đăng ký không cấp token**; **đăng nhập bị chặn (403 `EMAIL_NOT_VERIFIED`)** cho tới khi xác thực email qua OTP.
- **Tài khoản dịch vụ AI** nằm ở bảng riêng `ServiceAccounts`, không có email/password; xác thực bằng header `X-Api-Key`; key có thể thu hồi và tự hết hạn.
- **Quyền bán hàng (Seller) do Admin cấp**, không tự cấp khi đăng ký:
  - User mới đăng ký → `SellerStatus = None` → `POST /api/Items/sell` bị **403 `SELLER_NOT_APPROVED`**.
  - Chỉ endpoint `POST /api/Admin/seller-requests/{userId}/approve` (yêu cầu role `Admin`) mới mở khoá được.
  - `SellerStatus` được kiểm tra **trực tiếp từ DB** (không nhúng vào JWT) → có hiệu lực **tức thì**, không cần đăng nhập lại sau khi được duyệt.
  - Người bán vẫn là `Role = User` → mọi quyền mua / yêu thích / xem hồ sơ vẫn hoạt động bình thường.
  - Admin **không cần** xin quyền bán (đã có sẵn toàn bộ quyền).
  - **Luật nằm trong `SellerPermission`** (nguồn sự thật duy nhất) → áp dụng cho **cả tạo tin lẫn sửa tin**:
    - `POST /api/Items/sell` → seller chưa được duyệt bị **403**.
    - `PUT /api/Items/sell/{id}` → seller đã mất quyền bán **không sửa được** tin cũ.
  - **Từ chối yêu cầu / thu hồi quyền bán → gỡ toàn bộ tin đăng** của tài khoản đó khỏi sàn (soft delete hàng loạt).
  - **Không tạo được trạng thái mâu thuẫn**: duyệt rồi muốn bỏ quyền thì dùng `revoke` (`400 SELLER_USE_REVOKE_INSTEAD` nếu dùng nhầm `reject`).
  - **Không cấp quyền bán cho tài khoản đang bị cấm** → `400 BANNED_USER_CANNOT_BE_SELLER`.
- **Admin cấm tài khoản (`POST /api/Admin/users/{userId}/ban`)**:
  - `Status = Banned` → đăng nhập bị chặn **401 `ACCOUNT_BANNED`** kèm lý do.
  - Toàn bộ **refresh token bị thu hồi** → không lấy được access token mới.
  - **Quyền bán bị thu hồi** luôn (nếu đang có) → tài khoản bị khoá không đăng tin mới được.
  - **Middleware `BannedUserBlockMiddleware`** chặn ngay cả access token cũ còn hiệu lực (JWT stateless) → **403 `ACCOUNT_BANNED`**. Chỉ `SELECT Status, "BanReason"` theo PK mỗi request.
  - Admin **không thể** cấm/gỡ cấm tài khoản Admin → `400 ADMIN_CANNOT_BE_BANNED`.
  - Gỡ cấm: `POST /api/Admin/users/{userId}/unban` → `Status = Active`, đăng nhập lại được ngay.
  - **Cấm / thu hồi quyền bán → gỡ toàn bộ tin đăng của tài khoản khỏi sàn** (soft delete hàng loạt).
- **Vai trò quản trị (Role = Admin) — KHÔNG có API cấp / gỡ quyền**:
  - Vai trò được gán **trực tiếp trong DB** (tài khoản `admin` mặc định được seed khi khởi tạo).
  - **Không có đường leo thang quyền**: không endpoint nào cho phép Admin tự cấp quyền admin cho người khác, hay tự thu hồi quyền admin của người khác.
  - `RoleSyncMiddleware` vẫn giữ nguyên (tra `Role` từ DB mỗi request) → đổi role trực tiếp trong DB thì hiệu lực **tức thì**, không cần đăng nhập lại.
- **Hồ sơ là tài nguyên riêng tư (private):** `/api/Profile` yêu cầu đăng nhập (`[Authorize(Roles = "User, Admin")]`).
  - `userId` **luôn lấy từ access token**, không nhận từ client → không thể xem/sửa hồ sơ người khác.
  - Tài khoản dịch vụ AI (`AiService`, xác thực bằng API key) bị từ chối với **403 `FORBIDDEN`**.
  - Gửi API key kèm request đã đăng nhập → JWT được ưu tiên, API key bị bỏ qua.
- Swagger UI: http://localhost:&lt;port&gt;/swagger (Development) — nhấn **Authorize** để nhập token.

> 💡 **Lỗi 401 khi dùng Swagger?** Hai nguyên nhân phổ biến nhất:
>
> **1. Dán sai token.** Trong ô Authorize chỉ dán **giá trị `accessToken`**, tuyệt đối:
> - ❌ Không thêm chữ `Bearer` phía trước (Swagger tự thêm)
> - ❌ Không dán nguyên cả JSON response của `/api/Auth/login`
> - ❌ Không dán nhầm `refreshToken`
>
> Server ghi log lý do cụ thể trong console (`Token bi tu choi -> ...`), ví dụ `TOKEN_MALFORMED`, `INVALID_TOKEN_SIGNATURE`, `TOKEN_EXPIRED`.
>
> **2. `swagger.json` bị sai key của security requirement.** Khi đó Swagger UI **không gửi header `Authorization`** dù bạn đã Authorize thành công → 401 dù token hoàn toàn hợp lệ.
> Kiểm tra nhanh: mở `/swagger/v1/swagger.json`, phần `"security"` cuối file phải là `"Bearer": []` — **không phải** `"Bearer#/components/securitySchemes/Bearer"`.
> Nguyên nhân: `OpenApiSecuritySchemeReference(referenceId, hostDocument, externalResource)` — tham số thứ 3 là **externalResource**, truyền `"Bearer"` sẽ ghép thành `Bearer#/components/securitySchemes/Bearer`. Phải truyền `null`.

### 1.8 Tài khoản mặc định (seed)

| Username | Email | Mật khẩu | Vai trò |
|----------|-------|----------|---------|
| `admin` | `admin@sv.edu.vn` | `admin123` | Admin (đã xác thực email) |

> ⚠️ **Đổi mật khẩu `admin123` sau lần đăng nhập đầu tiên.**
> Seed chạy tự động khi khởi động API và là **idempotent** — không ghi đè dữ liệu đã có.
> Bỏ qua seed: đặt `"SkipDatabaseSeed": true` trong `appsettings.Development.json`.

> ℹ️ Khi bỏ seed demo + xóa dữ liệu cũ, các tài khoản mới đăng ký sẽ có `SellerStatus = None` và **phải được Admin duyệt** trước khi đăng tin bán.
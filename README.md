⚙️ STUDENT FLEA MARKET - BACKEND WEB API

An enterprise-ready, robust RESTful Web API powering the Student Flea Market platform. Build with .NET 8, Entity Framework Core and PostgreSQL, engineered to handle high-concurrency requests and integrate directly with AWS Cloud Infrastructure.

--------------------------------

🚀 Tech Stack & Core Libraries
- Runtime & Framework: .NET 10 Web API
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
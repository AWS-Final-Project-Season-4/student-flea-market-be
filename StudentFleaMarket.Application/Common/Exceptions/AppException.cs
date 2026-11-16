namespace StudentFleaMarket.Application.Common.Exceptions;

public class AppException : Exception
{
    public string Code { get; }

    public AppException(string message, string code = "APP_ERROR") : base(message)
    {
        Code = code;
    }
}

/// <summary>
/// Lỗi nghiệp vụ được map sang HTTP status code cụ thể.
/// </summary>
public class ValidationException : AppException
{
    public ValidationException(string message, string code = "VALIDATION_ERROR")
        : base(message, code) { }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message = "Không tìm thấy dữ liệu", string code = "NOT_FOUND")
        : base(message, code) { }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Không được phép truy cập", string code = "UNAUTHORIZED")
        : base(message, code) { }
}

/// <summary>
/// Người dùng đã xác thực danh tính nhưng chưa được phép thực hiện hành động này.
/// </summary>
public class ForbiddenException : AppException
{
    public ForbiddenException(string message, string code = "FORBIDDEN")
        : base(message, code) { }
}

public class ConflictException : AppException
{
    public ConflictException(string message, string code = "CONFLICT") : base(message, code) { }
}

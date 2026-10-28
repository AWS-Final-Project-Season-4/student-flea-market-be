using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.API.Contracts;
using StudentFleaMarket.Application.Auth;

namespace StudentFleaMarket.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(RegisterRequest request, IValidator<RegisterRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var result = await authService.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<AuthResponse>(result, "Registration successful."));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request, IValidator<LoginRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(new ApiResponse<AuthResponse>(await authService.LoginAsync(request, cancellationToken)));
    }
}

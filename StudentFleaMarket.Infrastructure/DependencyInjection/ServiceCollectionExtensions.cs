using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Auth;
using StudentFleaMarket.Application.DTOs.Profile;
using StudentFleaMarket.Application.Services;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Infrastructure.Repositories;
using StudentFleaMarket.Infrastructure.Services;

namespace StudentFleaMarket.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IServiceAccountService, ServiceAccountService>();
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<ISellerService, SellerService>();
        services.AddScoped<IAdminUserService, AdminUserService>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IServiceAccountRepository, ServiceAccountRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<IRepository<ApplicationUser>, EfRepository<ApplicationUser>>();
        services.AddScoped<IRepository<RefreshToken>, EfRepository<RefreshToken>>();
        services.AddScoped<IRepository<VerificationCode>, EfRepository<VerificationCode>>();
        services.AddScoped<IRepository<ServiceAccount>, EfRepository<ServiceAccount>>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmailSender, EmailSender>();

        // FluentValidation: tự động validate request DTO trong controller
        services.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();
        services.AddValidatorsFromAssemblyContaining<UpdateProfileRequestDtoValidator>();

        return services;
    }
}

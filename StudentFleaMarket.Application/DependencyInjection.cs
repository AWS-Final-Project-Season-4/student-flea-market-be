using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using StudentFleaMarket.Application.Auth;
using StudentFleaMarket.Application.Catalog;

namespace StudentFleaMarket.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddScoped<IValidator<CreateItemRequest>, CreateItemRequestValidator>();
        services.AddScoped<IValidator<UpdateItemRequest>, UpdateItemRequestValidator>();
        return services;
    }
}

using FluentValidation;

namespace StudentFleaMarket.Application.Catalog;

public sealed class CreateItemRequestValidator : AbstractValidator<CreateItemRequest>
{
    public CreateItemRequestValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(5000);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Condition).InclusiveBetween(1, 5);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(255);
        RuleForEach(x => x.ImageUrls).Must(BeHttpUrl).WithMessage("Each image URL must use HTTP or HTTPS.");
    }

    private static bool BeHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

public sealed class UpdateItemRequestValidator : AbstractValidator<UpdateItemRequest>
{
    public UpdateItemRequestValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(5000);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Condition).InclusiveBetween(1, 5);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(255);
        RuleForEach(x => x.ImageUrls).Must(BeHttpUrl).WithMessage("Each image URL must use HTTP or HTTPS.");
        RuleFor(x => x.Status).InclusiveBetween(1, 4);
    }

    private static bool BeHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

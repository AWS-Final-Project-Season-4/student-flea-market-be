using FluentValidation;

namespace StudentFleaMarket.Application.Media;

internal static class ImageFileRules
{
    internal static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    internal static readonly IReadOnlyDictionary<string, string[]> AllowedExtensions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = [".jpg", ".jpeg"],
            ["image/png"] = [".png"],
            ["image/webp"] = [".webp"]
        };

    internal static bool IsSafeFileName(string? name) =>
        !string.IsNullOrWhiteSpace(name) &&
        name == Path.GetFileName(name) &&
        !name.Contains('/') &&
        !name.Contains('\\');

    internal static bool ExtensionMatchesContentType(string? fileName, string? contentType)
    {
        if (fileName is null || contentType is null || !AllowedExtensions.TryGetValue(contentType, out var extensions))
            return false;
        var extension = Path.GetExtension(fileName);
        return extensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}

public sealed class PresignItemImagesRequestValidator : AbstractValidator<PresignItemImagesRequest>
{
    public PresignItemImagesRequestValidator(MediaUploadLimits limits)
    {
        RuleFor(x => x.Files)
            .NotNull()
            .Must(files => files is { Count: > 0 })
            .WithMessage("At least one image is required.")
            .Must(files => files is null || files.Count <= limits.MaxImagesPerItem)
            .WithMessage($"No more than {limits.MaxImagesPerItem} images can be requested at once.");

        RuleForEach(x => x.Files).ChildRules(file =>
        {
            file.RuleFor(x => x.FileName)
                .NotEmpty()
                .MaximumLength(255)
                .Must(ImageFileRules.IsSafeFileName)
                .WithMessage("File name must not contain a path.");
            file.RuleFor(x => x.ContentType)
                .NotEmpty()
                .Must(ImageFileRules.AllowedContentTypes.Contains)
                .WithMessage("Only image/jpeg, image/png, and image/webp are allowed.");
            file.RuleFor(x => x.SizeBytes)
                .GreaterThan(0)
                .LessThanOrEqualTo(limits.MaxFileSizeBytes);
            file.RuleFor(x => x).Must(f => ImageFileRules.ExtensionMatchesContentType(f.FileName, f.ContentType))
                .WithMessage("File extension does not match the declared content type.");
        });
    }
}

public sealed class PresignAvatarRequestValidator : AbstractValidator<PresignAvatarRequest>
{
    public PresignAvatarRequestValidator(MediaUploadLimits limits)
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255)
            .Must(ImageFileRules.IsSafeFileName)
            .WithMessage("File name must not contain a path.");
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ImageFileRules.AllowedContentTypes.Contains)
            .WithMessage("Only image/jpeg, image/png, and image/webp are allowed.");
        RuleFor(x => x.SizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(limits.MaxAvatarSizeBytes)
            .WithMessage($"Avatar must be at most {limits.MaxAvatarSizeBytes / (1024 * 1024)} MB.");
        RuleFor(x => x).Must(f => ImageFileRules.ExtensionMatchesContentType(f.FileName, f.ContentType))
            .WithMessage("File extension does not match the declared content type.");
    }
}

public sealed class ConfirmAvatarRequestValidator : AbstractValidator<ConfirmAvatarRequest>
{
    public ConfirmAvatarRequestValidator()
    {
        RuleFor(x => x.ObjectKey).NotEmpty().MaximumLength(512);
    }
}

public sealed class ConfirmItemImagesRequestValidator : AbstractValidator<ConfirmItemImagesRequest>
{
    public ConfirmItemImagesRequestValidator(MediaUploadLimits limits)
    {
        RuleFor(x => x.ObjectKeys)
            .NotNull()
            .Must(keys => keys is { Count: > 0 })
            .WithMessage("At least one object key is required.")
            .Must(keys => keys is null || keys.Count <= limits.MaxImagesPerItem)
            .WithMessage($"No more than {limits.MaxImagesPerItem} images can be confirmed at once.")
            .Must(keys => keys is null || keys.Distinct(StringComparer.Ordinal).Count() == keys.Count)
            .WithMessage("Object keys must be unique.");

        RuleForEach(x => x.ObjectKeys)
            .NotEmpty()
            .MaximumLength(512);
    }
}

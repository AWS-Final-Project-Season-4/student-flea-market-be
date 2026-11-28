namespace StudentFleaMarket.ImageProcessor;

/// <summary>
/// Raw upload key. Two shapes are accepted:
/// <list type="bullet">
/// <item><c>raw/users/{userId}/items/{itemId}/{uploadId}.{ext}</c> → <c>processed/items/{itemId}/{uploadId}</c></item>
/// <item><c>raw/users/{userId}/avatars/{uploadId}.{ext}</c> → <c>processed/avatars/{userId}/{uploadId}</c></item>
/// </list>
/// </summary>
public sealed record MediaObjectKey(Guid UserId, Guid? ItemId, Guid UploadId)
{
    public bool IsAvatar => ItemId is null;

    public string ProcessedPrefix => IsAvatar
        ? $"processed/avatars/{UserId:D}/{UploadId:D}"
        : $"processed/items/{ItemId:D}/{UploadId:D}";

    public static bool TryParseRaw(string key, out MediaObjectKey? value)
    {
        value = null;
        var segments = key.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 5 ||
            !segments[0].Equals("raw", StringComparison.Ordinal) ||
            !segments[1].Equals("users", StringComparison.Ordinal) ||
            !Guid.TryParseExact(segments[2], "D", out var userId))
            return false;

        Guid? itemId;
        string fileName;
        if (segments.Length == 6 && segments[3].Equals("items", StringComparison.Ordinal) &&
            Guid.TryParseExact(segments[4], "D", out var parsedItemId))
        {
            itemId = parsedItemId;
            fileName = segments[5];
        }
        else if (segments.Length == 5 && segments[3].Equals("avatars", StringComparison.Ordinal))
        {
            itemId = null;
            fileName = segments[4];
        }
        else
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);
        var uploadIdText = Path.GetFileNameWithoutExtension(fileName);
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp") ||
            !Guid.TryParseExact(uploadIdText, "D", out var uploadId))
            return false;

        value = new MediaObjectKey(userId, itemId, uploadId);
        return true;
    }
}

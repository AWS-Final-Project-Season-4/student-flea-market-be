using StudentFleaMarket.Application.Auth;
using StudentFleaMarket.Application.Catalog;
using Xunit;

namespace StudentFleaMarket.Tests;

public sealed class ValidationTests
{
    [Fact]
    public void Register_rejects_weak_password()
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest("Student", "student@example.com", "weak", null, null));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Password");
    }

    [Fact]
    public void Register_accepts_valid_input()
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest("Student Name", "student@example.com", "StrongPass1", "University", "S001"));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Create_item_rejects_negative_price()
    {
        var request = new CreateItemRequest(1, "Book", null, -1, 3, false, "Campus", []);
        Assert.False(new CreateItemRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Create_item_rejects_non_http_image_url()
    {
        var request = new CreateItemRequest(1, "Book", null, 10000, 3, false, "Campus", ["javascript:alert(1)"]);
        Assert.False(new CreateItemRequestValidator().Validate(request).IsValid);
    }
}

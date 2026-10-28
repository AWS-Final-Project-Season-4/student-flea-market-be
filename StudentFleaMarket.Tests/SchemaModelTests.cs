using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Infrastructure.Persistence;
using Xunit;

namespace StudentFleaMarket.Tests;

public sealed class SchemaModelTests
{
    private static readonly IModel Model = CreateContext().Model;

    [Fact]
    public void User_columns_follow_schema_contract()
    {
        var entity = Model.FindEntityType(typeof(ApplicationUser))!;
        Assert.False(entity.FindProperty(nameof(ApplicationUser.Email))!.IsNullable);
        Assert.False(entity.FindProperty(nameof(ApplicationUser.PasswordHash))!.IsNullable);
        Assert.False(entity.FindProperty(nameof(ApplicationUser.UpdatedAt))!.IsNullable);
        Assert.Equal("IsEmailVerified", entity.FindProperty(nameof(ApplicationUser.EmailConfirmed))!.GetColumnName());
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(ApplicationUser.NormalizedEmail));
    }

    [Fact]
    public void Exchange_request_nullability_and_expiry_follow_schema()
    {
        var entity = Model.FindEntityType(typeof(ExchangeRequest))!;
        Assert.True(entity.FindProperty(nameof(ExchangeRequest.OfferedItemId))!.IsNullable);
        Assert.True(entity.FindProperty(nameof(ExchangeRequest.AdditionalAmount))!.IsNullable);
        Assert.False(entity.FindProperty(nameof(ExchangeRequest.ExpiresAt))!.IsNullable);
    }

    [Fact]
    public void Transaction_and_payment_money_fields_follow_schema()
    {
        var transaction = Model.FindEntityType(typeof(Transaction))!;
        Assert.NotNull(transaction.FindProperty(nameof(Transaction.BuyerId)));
        Assert.NotNull(transaction.FindProperty(nameof(Transaction.SellerId)));
        Assert.NotNull(transaction.FindProperty(nameof(Transaction.TotalAmount)));
        Assert.NotNull(transaction.FindProperty(nameof(Transaction.Method)));

        var payment = Model.FindEntityType(typeof(Payment))!;
        Assert.Equal(typeof(int), payment.FindProperty(nameof(Payment.Amount))!.ClrType);
        Assert.False(payment.FindProperty(nameof(Payment.PaymentUrl))!.IsNullable);
    }

    [Fact]
    public void Messages_and_reviews_support_schema_relationships()
    {
        var message = Model.FindEntityType(typeof(Message))!;
        Assert.NotNull(message.FindProperty(nameof(Message.BuyerId)));

        var review = Model.FindEntityType(typeof(Review))!;
        Assert.DoesNotContain(review.GetIndexes(), index =>
            index.IsUnique && index.Properties.Count == 1 && index.Properties[0].Name == nameof(Review.TransactionId));
        Assert.Contains(review.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(x => x.Name).SequenceEqual(new[]
            {
                nameof(Review.TransactionId), nameof(Review.ReviewerId), nameof(Review.RevieweeId)
            }));
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=schema_model_test;Username=test;Password=test")
            .Options;
        return new AppDbContext(options);
    }
}

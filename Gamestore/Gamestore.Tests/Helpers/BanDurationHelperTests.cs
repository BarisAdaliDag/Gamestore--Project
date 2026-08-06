using Gamestore.BLL.Helpers;

namespace Gamestore.Tests.Helpers;

public class BanDurationHelperTests
{
    [Fact]
    public void GetDurationsShouldReturnFiveDurationsInOrder()
    {
        var result = BanDurationHelper.GetDurations();

        Assert.Equal(5, result.Count);
        Assert.Equal("1 hour", result[0]);
        Assert.Equal("1 day", result[1]);
        Assert.Equal("1 week", result[2]);
        Assert.Equal("1 month", result[3]);
        Assert.Equal("permanent", result[4]);
    }

    [Theory]
    [InlineData("1 hour")]
    [InlineData("1 day")]
    [InlineData("1 week")]
    [InlineData("1 month")]
    [InlineData("permanent")]
    public void IsSupportedShouldReturnTrueForValidDurations(string duration)
    {
        Assert.True(BanDurationHelper.IsSupported(duration));
    }

    [Theory]
    [InlineData("2 years")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void IsSupportedShouldReturnFalseForInvalidDurations(string? duration)
    {
        Assert.False(BanDurationHelper.IsSupported(duration));
    }

    [Theory]
    [InlineData("1 Hour")]
    [InlineData("1 HOUR")]
    [InlineData("Permanent")]
    [InlineData("PERMANENT")]
    public void IsSupportedShouldBeCaseInsensitive(string duration)
    {
        Assert.True(BanDurationHelper.IsSupported(duration));
    }

    [Fact]
    public void ResolveShouldAddOneHourForOneHourDuration()
    {
        var from = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        var result = BanDurationHelper.Resolve("1 hour", from);

        Assert.Equal(from.AddHours(1), result);
    }

    [Fact]
    public void ResolveShouldAddOneDayForOneDayDuration()
    {
        var from = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        var result = BanDurationHelper.Resolve("1 day", from);

        Assert.Equal(from.AddDays(1), result);
    }

    [Fact]
    public void ResolveShouldAddSevenDaysForOneWeekDuration()
    {
        var from = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        var result = BanDurationHelper.Resolve("1 week", from);

        Assert.Equal(from.AddDays(7), result);
    }

    [Fact]
    public void ResolveShouldUseAddMonthsNotAddDaysForOneMonthDuration()
    {
        // Jan 31 + AddMonths(1) = Feb 29 (leap year), not Mar 2 (which AddDays(30) would give)
        var from = new DateTime(2024, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        var result = BanDurationHelper.Resolve("1 month", from);

        Assert.Equal(new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void ResolveShouldReturnNullForPermanentDuration()
    {
        var from = DateTime.UtcNow;

        var result = BanDurationHelper.Resolve("permanent", from);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveShouldBeCaseInsensitive()
    {
        var from = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        var result = BanDurationHelper.Resolve("1 HOUR", from);

        Assert.Equal(from.AddHours(1), result);
    }

    [Fact]
    public void ResolveShouldThrowArgumentExceptionForUnsupportedDuration()
    {
        var from = DateTime.UtcNow;

        Assert.Throws<ArgumentException>(() => BanDurationHelper.Resolve("2 years", from));
    }

    [Fact]
    public void PermanentConstantShouldBeLowerCaseString()
    {
        Assert.Equal("permanent", BanDurationHelper.Permanent);
    }
}
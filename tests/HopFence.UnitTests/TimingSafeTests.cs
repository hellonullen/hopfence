using HopFence.AspNetCore;
using Xunit;

namespace HopFence.UnitTests;

public class TimingSafeTests
{
    [Theory]
    [InlineData("my-secret-token", "my-secret-token", true)]
    [InlineData("my-secret-token", "wrong-secret-token", false)]
    [InlineData("my-secret-token", "my-secret-toke", false)]
    [InlineData("short", "much-longer-token-value", false)]
    [InlineData("", "", true)]
    public void FixedTimeEquals_ComparesCorrectly(string tokenA, string tokenB, bool expected)
    {
        bool result = HopFenceProbeHandler.FixedTimeEquals(tokenA, tokenB);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FixedTimeEquals_HandlesNullGracefully()
    {
        Assert.False(HopFenceProbeHandler.FixedTimeEquals(null!, "token"));
        Assert.False(HopFenceProbeHandler.FixedTimeEquals("token", null!));
        Assert.False(HopFenceProbeHandler.FixedTimeEquals(null!, null!));
    }
}

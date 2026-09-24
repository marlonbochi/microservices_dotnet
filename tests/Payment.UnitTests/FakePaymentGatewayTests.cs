using Microsoft.Extensions.Options;
using Payment.Worker.Gateway;

namespace Payment.UnitTests;

public sealed class FakePaymentGatewayTests
{
    private static FakePaymentGateway CreateGateway(decimal cardLimit = 1_000m) =>
        new(Options.Create(new PaymentOptions { ProcessingDelay = TimeSpan.Zero, CardLimit = cardLimit }), TimeProvider.System);

    [Fact]
    public async Task ChargeAsync_WithinLimit_Approves()
    {
        var result = await CreateGateway().ChargeAsync(new PaymentRequest(Guid.NewGuid(), 100m, false), TestContext.Current.CancellationToken);

        result.Approved.ShouldBeTrue();
        result.TransactionId.ShouldStartWith("TX-");
    }

    [Fact]
    public async Task ChargeAsync_WithSimulatedFailure_Declines()
    {
        var result = await CreateGateway().ChargeAsync(new PaymentRequest(Guid.NewGuid(), 100m, true), TestContext.Current.CancellationToken);

        result.Approved.ShouldBeFalse();
        result.DeclineReason.ShouldBe(FakePaymentGateway.SimulatedDeclineReason);
    }

    [Fact]
    public async Task ChargeAsync_AboveCardLimit_Declines()
    {
        var result = await CreateGateway(cardLimit: 50m).ChargeAsync(new PaymentRequest(Guid.NewGuid(), 51m, false), TestContext.Current.CancellationToken);

        result.Approved.ShouldBeFalse();
        result.DeclineReason!.ShouldContain("card limit");
    }
}

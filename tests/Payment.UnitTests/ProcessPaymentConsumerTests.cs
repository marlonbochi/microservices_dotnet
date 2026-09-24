using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Payment.Worker.Consumers;
using Payment.Worker.Gateway;
using Store.Contracts.Payment;

namespace Payment.UnitTests;

public sealed class ProcessPaymentConsumerTests : IAsyncLifetime
{
    private readonly IPaymentGateway _gateway = Substitute.For<IPaymentGateway>();
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddSingleton(_gateway)
            .AddMassTransitTestHarness(bus => bus.AddConsumer<ProcessPaymentConsumer>())
            .BuildServiceProvider(validateScopes: true);
        _harness = _provider.GetTestHarness();
        await _harness.Start();
    }

    public async ValueTask DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Consume_WhenGatewayApproves_PublishesPaymentApproved()
    {
        var orderId = Guid.NewGuid();
        _gateway.ChargeAsync(Arg.Any<PaymentRequest>(), Arg.Any<CancellationToken>()).Returns(PaymentResult.Approve("TX-1"));

        await _harness.Bus.Publish(new ProcessPayment(orderId, 10m, false), Token);

        (await _harness.Published.Any<PaymentApproved>(message => message.Context.Message == new PaymentApproved(orderId, "TX-1"), Token))
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Consume_WhenGatewayDeclines_PublishesPaymentDeclined()
    {
        var orderId = Guid.NewGuid();
        _gateway.ChargeAsync(Arg.Any<PaymentRequest>(), Arg.Any<CancellationToken>()).Returns(PaymentResult.Decline("no funds"));

        await _harness.Bus.Publish(new ProcessPayment(orderId, 10m, true), Token);

        (await _harness.Published.Any<PaymentDeclined>(message => message.Context.Message.Reason == "no funds", Token)).ShouldBeTrue();
        (await _harness.Published.Any<PaymentApproved>(Token)).ShouldBeFalse();
    }
}

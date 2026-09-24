using Inventory.Application.Reservations;
using Inventory.Application.Stock;
using Inventory.Infrastructure.Messaging.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Store.Contracts.Catalog;
using Store.Contracts.Inventory;
using Store.Contracts.Ordering;
using Store.SharedKernel;

namespace Inventory.UnitTests.Messaging;

/// <summary>
/// Uses the MassTransit in-memory test harness: real serialization and pipeline, no broker.
/// Verifies that each consumer translates its message into the right application command.
/// </summary>
public sealed class ConsumerTests : IAsyncLifetime
{
    private readonly ICommandHandler<RegisterProductStockCommand> _register = Substitute.For<ICommandHandler<RegisterProductStockCommand>>();
    private readonly ICommandHandler<RenameProductCommand> _rename = Substitute.For<ICommandHandler<RenameProductCommand>>();
    private readonly ICommandHandler<ReserveStockCommand> _reserve = Substitute.For<ICommandHandler<ReserveStockCommand>>();
    private readonly ICommandHandler<CommitStockCommand> _commit = Substitute.For<ICommandHandler<CommitStockCommand>>();
    private readonly ICommandHandler<ReleaseStockCommand> _release = Substitute.For<ICommandHandler<ReleaseStockCommand>>();
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    public async ValueTask InitializeAsync()
    {
        _rename.HandleAsync(default!, default).ReturnsForAnyArgs(Result.Success());
        _provider = new ServiceCollection()
            .AddSingleton(_register).AddSingleton(_rename).AddSingleton(_reserve).AddSingleton(_commit).AddSingleton(_release)
            .AddMassTransitTestHarness(bus =>
            {
                bus.AddConsumer<ProductCreatedConsumer>();
                bus.AddConsumer<ProductUpdatedConsumer>();
                bus.AddConsumer<ReserveStockConsumer>();
                bus.AddConsumer<CommitStockConsumer>();
                bus.AddConsumer<ReleaseStockConsumer>();
            })
            .BuildServiceProvider(validateScopes: true);
        _harness = _provider.GetTestHarness();
        await _harness.Start();
    }

    public async ValueTask DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task ProductCreated_RegistersStockWithInitialQuantity()
    {
        var message = new ProductCreated(Guid.NewGuid(), "KB-001", "Teclado", 350m, 10, DateTimeOffset.UtcNow);

        await _harness.Bus.Publish(message, TestContext.Current.CancellationToken);

        (await _harness.GetConsumerHarness<ProductCreatedConsumer>().Consumed.Any<ProductCreated>(TestContext.Current.CancellationToken)).ShouldBeTrue();
        await _register.Received(1).HandleAsync(
            new RegisterProductStockCommand(message.ProductId, "KB-001", "Teclado", 10), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProductUpdated_RenamesStockItem()
    {
        var message = new ProductUpdated(Guid.NewGuid(), "Novo nome", 10m, DateTimeOffset.UtcNow);

        await _harness.Bus.Publish(message, TestContext.Current.CancellationToken);

        (await _harness.GetConsumerHarness<ProductUpdatedConsumer>().Consumed.Any<ProductUpdated>(TestContext.Current.CancellationToken)).ShouldBeTrue();
        await _rename.Received(1).HandleAsync(new RenameProductCommand(message.ProductId, "Novo nome"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReserveStock_MapsOrderLinesToReservationLines()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        await _harness.Bus.Publish(new ReserveStock(orderId, [new OrderLine(productId, 2)]), TestContext.Current.CancellationToken);

        (await _harness.GetConsumerHarness<ReserveStockConsumer>().Consumed.Any<ReserveStock>(TestContext.Current.CancellationToken)).ShouldBeTrue();
        await _reserve.Received(1).HandleAsync(
            Arg.Is<ReserveStockCommand>(command =>
                command.OrderId == orderId && command.Lines.Single().ProductId == productId && command.Lines.Single().Quantity == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CommitAndRelease_DelegateToHandlers()
    {
        var orderId = Guid.NewGuid();

        await _harness.Bus.Publish(new CommitStock(orderId), TestContext.Current.CancellationToken);
        await _harness.Bus.Publish(new ReleaseStock(orderId), TestContext.Current.CancellationToken);

        (await _harness.GetConsumerHarness<CommitStockConsumer>().Consumed.Any<CommitStock>(TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await _harness.GetConsumerHarness<ReleaseStockConsumer>().Consumed.Any<ReleaseStock>(TestContext.Current.CancellationToken)).ShouldBeTrue();
        await _commit.Received(1).HandleAsync(new CommitStockCommand(orderId), Arg.Any<CancellationToken>());
        await _release.Received(1).HandleAsync(new ReleaseStockCommand(orderId), Arg.Any<CancellationToken>());
    }
}

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ordering.Infrastructure.Messaging.Sagas;

internal sealed class OrderStateMap : SagaClassMap<OrderState>
{
    protected override void Configure(EntityTypeBuilder<OrderState> entity, ModelBuilder model)
    {
        entity.ToTable("OrderSagas");
        entity.Property(state => state.CurrentState).HasMaxLength(64);
        entity.Property(state => state.Total).HasPrecision(18, 2);
        entity.Property(state => state.FailureReason).HasMaxLength(500);
        entity.Property(state => state.RowVersion).IsRowVersion();
    }
}

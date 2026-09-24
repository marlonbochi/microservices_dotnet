using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Ordering.Application.Abstractions;

namespace Ordering.Infrastructure.Realtime;

/// <summary>Best-effort push: a failed notification must never fail the business transaction.</summary>
internal sealed partial class SignalROrderNotifier(
    IHubContext<OrdersHub> hubContext,
    ILogger<SignalROrderNotifier> logger) : IOrderNotifier
{
    public async Task NotifyStatusChangedAsync(OrderStatusNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            await hubContext.Clients.All.SendAsync(OrdersHub.StatusChangedMethod, notification, cancellationToken);
        }
        catch (Exception exception)
        {
            LogPushFailed(logger, notification.OrderId, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not push status of order {OrderId}")]
    private static partial void LogPushFailed(ILogger logger, Guid orderId, Exception exception);
}

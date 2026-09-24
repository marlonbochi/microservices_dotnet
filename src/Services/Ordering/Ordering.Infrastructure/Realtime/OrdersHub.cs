using Microsoft.AspNetCore.SignalR;

namespace Ordering.Infrastructure.Realtime;

/// <summary>SignalR hub: browsers connect to receive order status changes in real time.</summary>
public sealed class OrdersHub : Hub
{
    public const string Path = "/hubs/orders";
    public const string StatusChangedMethod = "OrderStatusChanged";
}

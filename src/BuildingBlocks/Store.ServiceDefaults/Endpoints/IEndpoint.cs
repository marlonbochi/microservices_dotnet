using Microsoft.AspNetCore.Routing;

namespace Store.ServiceDefaults.Endpoints;

/// <summary>
/// One HTTP endpoint = one class (Single Responsibility). Implementations are discovered
/// by reflection, so adding an endpoint never requires editing Program.cs (Open/Closed).
/// </summary>
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}

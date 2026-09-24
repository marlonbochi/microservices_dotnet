using Catalog.Application.Products;
using Catalog.Application.Products.Create;
using Catalog.Application.Products.Queries;
using Catalog.Application.Products.Update;
using Microsoft.Extensions.DependencyInjection;
using Store.SharedKernel;

namespace Catalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateProductCommand, ProductResponse>, CreateProductHandler>();
        services.AddScoped<ICommandHandler<UpdateProductCommand, ProductResponse>, UpdateProductHandler>();
        services.AddScoped<IQueryHandler<GetProductByIdQuery, ProductResponse?>, GetProductByIdHandler>();
        services.AddScoped<IQueryHandler<ListProductsQuery, IReadOnlyList<ProductResponse>>, ListProductsHandler>();
        services.AddScoped<IQueryHandler<GetProductsByIdsQuery, IReadOnlyList<ProductResponse>>, GetProductsByIdsHandler>();
        return services;
    }
}

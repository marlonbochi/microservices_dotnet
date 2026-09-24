using Catalog.Application;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Persistence;
using FluentValidation;
using Store.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddWebDefaults(typeof(Program).Assembly);
builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);
builder.Services.AddCatalogApplication();
builder.AddCatalogInfrastructure();

var app = builder.Build();

app.UseWebDefaults();
await app.ApplyMigrationsAsync<CatalogDbContext>();

await app.RunAsync();

public partial class Program;

using FluentValidation;
using Inventory.Application;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Persistence;
using Store.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddWebDefaults(typeof(Program).Assembly);
builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);
builder.Services.AddInventoryApplication();
builder.AddInventoryInfrastructure();

var app = builder.Build();

app.UseWebDefaults();
await app.ApplyMigrationsAsync<InventoryDbContext>();

await app.RunAsync();

public partial class Program;

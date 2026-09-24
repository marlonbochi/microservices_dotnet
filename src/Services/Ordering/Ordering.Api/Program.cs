using FluentValidation;
using Ordering.Application;
using Ordering.Infrastructure;
using Ordering.Infrastructure.Persistence;
using Ordering.Infrastructure.Realtime;
using Store.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddWebDefaults(typeof(Program).Assembly);
builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);
builder.Services.AddOrderingApplication();
builder.AddOrderingInfrastructure();

var app = builder.Build();

app.UseWebDefaults();
app.MapHub<OrdersHub>(OrdersHub.Path);
await app.ApplyMigrationsAsync<OrderingDbContext>();

await app.RunAsync();

public partial class Program;

using Payment.Worker.Consumers;
using Payment.Worker.Gateway;
using Store.ServiceDefaults;
using Store.ServiceDefaults.Messaging;

// A web host (instead of a plain worker) only to expose /health endpoints; there is no public API.
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.Configure<PaymentOptions>(builder.Configuration.GetSection(PaymentOptions.SectionName));
builder.Services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
builder.AddStoreMessaging(bus => bus.AddConsumer<ProcessPaymentConsumer>());

var app = builder.Build();

app.MapDefaultEndpoints();

await app.RunAsync();

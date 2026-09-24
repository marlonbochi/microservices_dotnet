using Store.ServiceDefaults;

// API Gateway: the single entry point for the frontend (routing, CORS, WebSockets for SignalR).
var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.AddServiceDefaults();
builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseCors(FrontendCorsPolicy);
app.UseWebSockets();
app.MapDefaultEndpoints();
app.MapReverseProxy();

await app.RunAsync();

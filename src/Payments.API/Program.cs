using Microsoft.EntityFrameworkCore;
using Payments.API.Data;
using Payments.API.Services;
using Payments.API.Clients;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseInMemoryDatabase("PaymentDb"));

// Configure HTTP Clients with Polly
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

// Customer Service Client
builder.Services.AddHttpClient<ICustomerServiceClient, CustomerServiceClient>(client =>
{
    var baseUrl = builder.Configuration["ServiceUrls:CustomerService"] ?? "http://localhost:5001";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(circuitBreakerPolicy);

// Loan Service Client
builder.Services.AddHttpClient<ILoanServiceClient, LoanServiceClient>(client =>
{
    var baseUrl = builder.Configuration["ServiceUrls:LoanService"] ?? "http://localhost:5003";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(circuitBreakerPolicy);

// Add Services
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Payments Service API", 
        Version = "v1",
        Description = "Microservice for managing payments. Depends on Customer and Loans services."
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    context.Database.EnsureCreated();
}

var port = Environment.GetEnvironmentVariable("PORT") ?? "5004";
app.Urls.Add($"http://+:{port}");

Console.WriteLine($"Payments Service starting on port {port}...");
Console.WriteLine("Dependencies:");
Console.WriteLine($"  - Customer Service: {builder.Configuration["ServiceUrls:CustomerService"] ?? "http://localhost:5001"}");
Console.WriteLine($"  - Loans Service: {builder.Configuration["ServiceUrls:LoanService"] ?? "http://localhost:5003"}");

app.Run();

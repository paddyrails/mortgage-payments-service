using Microsoft.AspNetCore.Mvc;
using Payments.API.Clients;

namespace Payments.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ICustomerServiceClient _customerClient;
    private readonly ILoanServiceClient _loanClient;

    public HealthController(ICustomerServiceClient customerClient, ILoanServiceClient loanClient)
    {
        _customerClient = customerClient;
        _loanClient = loanClient;
    }

    [HttpGet]
    public IActionResult Health() => Ok(new 
    { 
        Status = "Healthy", 
        Service = "Payments.API", 
        Timestamp = DateTime.UtcNow,
        Dependencies = new[] { "Customer.API", "Loans.API" }
    });

    [HttpGet("live")]
    public IActionResult Live() => Ok(new { Status = "Alive" });

    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        var customerHealthy = await CheckServiceAsync(() => _customerClient.CustomerExistsAsync(Guid.Empty));
        var loanHealthy = await CheckServiceAsync(() => _loanClient.LoanExistsAsync(Guid.Empty));

        var status = new
        {
            Status = customerHealthy && loanHealthy ? "Ready" : "Degraded",
            Dependencies = new
            {
                CustomerService = customerHealthy ? "Healthy" : "Unhealthy",
                LoansService = loanHealthy ? "Healthy" : "Unhealthy"
            }
        };

        return customerHealthy && loanHealthy ? Ok(status) : StatusCode(503, status);
    }

    private async Task<bool> CheckServiceAsync(Func<Task<bool>> check)
    {
        try { await check(); return true; }
        catch { return false; }
    }
}

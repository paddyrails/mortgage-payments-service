using System.Net.Http.Json;

namespace Payments.API.Clients;

public class CustomerServiceClient : ICustomerServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomerServiceClient> _logger;

    public CustomerServiceClient(HttpClient httpClient, ILogger<CustomerServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CustomerDto?> GetCustomerAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<CustomerApiResponse<CustomerDto>>(
                $"/api/customers/{customerId}");
            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer {CustomerId}", customerId);
            return null;
        }
    }

    public async Task<bool> CustomerExistsAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/customers/{customerId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking customer {CustomerId}", customerId);
            return false;
        }
    }
}

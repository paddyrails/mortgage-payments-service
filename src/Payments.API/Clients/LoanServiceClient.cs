using System.Net.Http.Json;

namespace Payments.API.Clients;

public class LoanServiceClient : ILoanServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LoanServiceClient> _logger;

    public LoanServiceClient(HttpClient httpClient, ILogger<LoanServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LoanDto?> GetLoanAsync(Guid loanId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<LoanApiResponse<LoanDto>>(
                $"/api/loans/{loanId}?enrich=false");
            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching loan {LoanId}", loanId);
            return null;
        }
    }

    public async Task<LoanBalanceDto?> GetLoanBalanceAsync(Guid loanId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<LoanApiResponse<LoanBalanceDto>>(
                $"/api/loans/{loanId}/balance");
            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching loan balance {LoanId}", loanId);
            return null;
        }
    }

    public async Task<IEnumerable<LoanDto>> GetLoansByCustomerAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<LoanApiResponse<IEnumerable<LoanDto>>>(
                $"/api/loans/customer/{customerId}");
            return response?.Success == true ? response.Data ?? Enumerable.Empty<LoanDto>() : Enumerable.Empty<LoanDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching loans for customer {CustomerId}", customerId);
            return Enumerable.Empty<LoanDto>();
        }
    }

    public async Task<bool> ApplyPaymentAsync(Guid loanId, decimal principalAmount, decimal interestAmount)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/loans/{loanId}/apply-payment?principal={principalAmount}&interest={interestAmount}",
                null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying payment to loan {LoanId}", loanId);
            return false;
        }
    }

    public async Task<bool> LoanExistsAsync(Guid loanId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/loans/{loanId}?enrich=false");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking loan {LoanId}", loanId);
            return false;
        }
    }
}

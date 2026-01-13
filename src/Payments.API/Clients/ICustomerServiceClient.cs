namespace Payments.API.Clients;

public interface ICustomerServiceClient
{
    Task<CustomerDto?> GetCustomerAsync(Guid customerId);
    Task<bool> CustomerExistsAsync(Guid customerId);
}

public record CustomerDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
}

public record CustomerApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
}

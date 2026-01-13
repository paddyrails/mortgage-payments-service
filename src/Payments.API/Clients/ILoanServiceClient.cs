namespace Payments.API.Clients;

public interface ILoanServiceClient
{
    Task<LoanDto?> GetLoanAsync(Guid loanId);
    Task<LoanBalanceDto?> GetLoanBalanceAsync(Guid loanId);
    Task<IEnumerable<LoanDto>> GetLoansByCustomerAsync(Guid customerId);
    Task<bool> ApplyPaymentAsync(Guid loanId, decimal principalAmount, decimal interestAmount);
    Task<bool> LoanExistsAsync(Guid loanId);
}

public record LoanDto
{
    public Guid Id { get; init; }
    public string LoanNumber { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public Guid PropertyId { get; init; }
    public decimal PrincipalAmount { get; init; }
    public decimal InterestRate { get; init; }
    public int TermMonths { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal MonthlyPayment { get; init; }
    public decimal CurrentBalance { get; init; }
}

public record LoanBalanceDto
{
    public Guid LoanId { get; init; }
    public string LoanNumber { get; init; } = string.Empty;
    public decimal CurrentBalance { get; init; }
    public decimal PrincipalPaid { get; init; }
    public decimal InterestPaid { get; init; }
    public int PaymentsMade { get; init; }
    public int PaymentsRemaining { get; init; }
    public DateTime? NextPaymentDate { get; init; }
    public decimal NextPaymentAmount { get; init; }
}

public record LoanApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
}

using Microsoft.EntityFrameworkCore;
using Payments.API.Data;
using Payments.API.DTOs;
using Payments.API.Models;
using Payments.API.Clients;

namespace Payments.API.Services;

public class PaymentService : IPaymentService
{
    private readonly PaymentDbContext _context;
    private readonly ICustomerServiceClient _customerClient;
    private readonly ILoanServiceClient _loanClient;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        PaymentDbContext context,
        ICustomerServiceClient customerClient,
        ILoanServiceClient loanClient,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _customerClient = customerClient;
        _loanClient = loanClient;
        _logger = logger;
    }

    public async Task<IEnumerable<PaymentSummaryDto>> GetAllPaymentsAsync()
    {
        var payments = await _context.Payments
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return payments.Select(MapToSummaryDto);
    }

    public async Task<PaymentResponseDto?> GetPaymentByIdAsync(Guid id)
    {
        var payment = await _context.Payments
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null) return null;

        return await MapToResponseDtoAsync(payment);
    }

    public async Task<IEnumerable<PaymentSummaryDto>> GetPaymentsByLoanAsync(Guid loanId)
    {
        var payments = await _context.Payments
            .Where(p => p.LoanId == loanId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return payments.Select(MapToSummaryDto);
    }

    public async Task<IEnumerable<PaymentSummaryDto>> GetPaymentsByCustomerAsync(Guid customerId)
    {
        var payments = await _context.Payments
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return payments.Select(MapToSummaryDto);
    }

    public async Task<PaymentHistoryDto?> GetPaymentHistoryAsync(Guid loanId)
    {
        var loan = await _loanClient.GetLoanAsync(loanId);
        if (loan == null) return null;

        var payments = await _context.Payments
            .Where(p => p.LoanId == loanId && p.Status == PaymentStatus.Completed)
            .OrderByDescending(p => p.ProcessedDate)
            .ToListAsync();

        return new PaymentHistoryDto
        {
            LoanId = loanId,
            LoanNumber = loan.LoanNumber,
            TotalPaid = payments.Sum(p => p.Amount),
            TotalPrincipalPaid = payments.Sum(p => p.PrincipalAmount + p.AdditionalPrincipal),
            TotalInterestPaid = payments.Sum(p => p.InterestAmount),
            TotalLateFeesPaid = payments.Sum(p => p.LateFeeAmount),
            PaymentCount = payments.Count,
            LastPaymentDate = payments.FirstOrDefault()?.ProcessedDate,
            Payments = payments.Select(MapToSummaryDto).ToList()
        };
    }

    public async Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentDto dto)
    {
        // Validate loan exists
        var loan = await _loanClient.GetLoanAsync(dto.LoanId);
        if (loan == null)
            throw new InvalidOperationException($"Loan {dto.LoanId} not found");

        // Validate customer exists
        var customerExists = await _customerClient.CustomerExistsAsync(dto.CustomerId);
        if (!customerExists)
            throw new InvalidOperationException($"Customer {dto.CustomerId} not found");

        // Calculate payment breakdown
        var loanBalance = await _loanClient.GetLoanBalanceAsync(dto.LoanId);
        var interestRate = loan.InterestRate / 100 / 12;
        var interestAmount = Math.Round(loan.CurrentBalance * interestRate, 2);
        var principalAmount = Math.Round(dto.Amount - interestAmount, 2);

        var payment = new Payment
        {
            PaymentNumber = await GeneratePaymentNumberAsync(),
            LoanId = dto.LoanId,
            CustomerId = dto.CustomerId,
            Amount = dto.Amount,
            PrincipalAmount = Math.Max(0, principalAmount),
            InterestAmount = interestAmount,
            EscrowAmount = 0, // Could be calculated from loan escrow settings
            AdditionalPrincipal = dto.AdditionalPrincipal,
            Status = dto.ScheduledDate.HasValue ? PaymentStatus.Scheduled : PaymentStatus.Pending,
            PaymentType = dto.PaymentType,
            PaymentMethod = dto.PaymentMethod,
            ScheduledDate = dto.ScheduledDate,
            DueDate = loanBalance?.NextPaymentDate,
            BankAccountLast4 = dto.BankAccountLast4,
            Notes = dto.Notes
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created payment {PaymentNumber} for loan {LoanId}", 
            payment.PaymentNumber, dto.LoanId);

        return await MapToResponseDtoAsync(payment);
    }

    public async Task<PaymentResponseDto?> ProcessPaymentAsync(Guid paymentId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null) return null;

        if (payment.Status != PaymentStatus.Pending && payment.Status != PaymentStatus.Scheduled)
        {
            throw new InvalidOperationException($"Payment {paymentId} cannot be processed. Current status: {payment.Status}");
        }

        // Update payment status
        payment.Status = PaymentStatus.Processing;
        await _context.SaveChangesAsync();

        try
        {
            // Apply payment to loan
            var totalPrincipal = payment.PrincipalAmount + payment.AdditionalPrincipal;
            var applied = await _loanClient.ApplyPaymentAsync(payment.LoanId, totalPrincipal, payment.InterestAmount);

            if (applied)
            {
                payment.Status = PaymentStatus.Completed;
                payment.ProcessedDate = DateTime.UtcNow;
                payment.ConfirmationNumber = GenerateConfirmationNumber();

                // Create transaction record
                var transaction = new PaymentTransaction
                {
                    PaymentId = payment.Id,
                    TransactionNumber = $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                    TransactionType = TransactionType.Debit,
                    Amount = payment.Amount,
                    Status = TransactionStatus.Completed,
                    Description = $"Payment processed for loan"
                };
                _context.Transactions.Add(transaction);

                _logger.LogInformation("Processed payment {PaymentNumber}", payment.PaymentNumber);
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = "Failed to apply payment to loan";
                _logger.LogWarning("Failed to process payment {PaymentNumber}", payment.PaymentNumber);
            }
        }
        catch (Exception ex)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = ex.Message;
            _logger.LogError(ex, "Error processing payment {PaymentNumber}", payment.PaymentNumber);
        }

        payment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await MapToResponseDtoAsync(payment);
    }

    public async Task<bool> CancelPaymentAsync(Guid id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null) return false;

        if (payment.Status == PaymentStatus.Completed || payment.Status == PaymentStatus.Processing)
        {
            throw new InvalidOperationException("Cannot cancel a completed or processing payment");
        }

        payment.Status = PaymentStatus.Cancelled;
        payment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<PaymentScheduleResponseDto?> GetPaymentScheduleAsync(Guid loanId)
    {
        var schedule = await _context.PaymentSchedules
            .FirstOrDefaultAsync(s => s.LoanId == loanId && s.IsActive);

        return schedule == null ? null : MapScheduleToDto(schedule);
    }

    public async Task<PaymentScheduleResponseDto> CreatePaymentScheduleAsync(CreatePaymentScheduleDto dto)
    {
        // Deactivate existing schedule
        var existing = await _context.PaymentSchedules
            .FirstOrDefaultAsync(s => s.LoanId == dto.LoanId && s.IsActive);
        
        if (existing != null)
        {
            existing.IsActive = false;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        var schedule = new PaymentSchedule
        {
            LoanId = dto.LoanId,
            CustomerId = dto.CustomerId,
            IsAutoPay = dto.IsAutoPay,
            PreferredPaymentMethod = dto.PreferredPaymentMethod,
            BankAccountNumber = dto.BankAccountNumber,
            RoutingNumber = dto.RoutingNumber,
            PaymentDayOfMonth = dto.PaymentDayOfMonth,
            RegularPaymentAmount = dto.RegularPaymentAmount,
            NextPaymentDate = CalculateNextPaymentDate(dto.PaymentDayOfMonth),
            IsActive = true
        };

        _context.PaymentSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        return MapScheduleToDto(schedule);
    }

    public async Task<PaymentScheduleResponseDto?> UpdateAutoPayAsync(Guid loanId, bool enabled)
    {
        var schedule = await _context.PaymentSchedules
            .FirstOrDefaultAsync(s => s.LoanId == loanId && s.IsActive);

        if (schedule == null) return null;

        schedule.IsAutoPay = enabled;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapScheduleToDto(schedule);
    }

    public async Task<IEnumerable<LateFeeResponseDto>> GetLateFeesByLoanAsync(Guid loanId)
    {
        var fees = await _context.LateFees
            .Where(f => f.LoanId == loanId)
            .OrderByDescending(f => f.DueDate)
            .ToListAsync();

        return fees.Select(f => new LateFeeResponseDto
        {
            Id = f.Id,
            LoanId = f.LoanId,
            CustomerId = f.CustomerId,
            Amount = f.Amount,
            DueDate = f.DueDate,
            AssessedDate = f.AssessedDate,
            IsPaid = f.IsPaid,
            PaidDate = f.PaidDate
        });
    }

    public async Task<LateFeeResponseDto> AssessLateFeeAsync(Guid loanId, decimal amount)
    {
        var loan = await _loanClient.GetLoanAsync(loanId);
        if (loan == null)
            throw new InvalidOperationException($"Loan {loanId} not found");

        var lateFee = new LateFee
        {
            LoanId = loanId,
            CustomerId = loan.CustomerId,
            Amount = amount,
            DueDate = DateTime.UtcNow,
            AssessedDate = DateTime.UtcNow,
            IsPaid = false
        };

        _context.LateFees.Add(lateFee);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Assessed late fee of {Amount} for loan {LoanId}", amount, loanId);

        return new LateFeeResponseDto
        {
            Id = lateFee.Id,
            LoanId = lateFee.LoanId,
            CustomerId = lateFee.CustomerId,
            Amount = lateFee.Amount,
            DueDate = lateFee.DueDate,
            AssessedDate = lateFee.AssessedDate,
            IsPaid = lateFee.IsPaid
        };
    }

    #region Private Helper Methods

    private async Task<string> GeneratePaymentNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var count = await _context.Payments.CountAsync(p => p.PaymentNumber.StartsWith($"PMT-{year}"));
        return $"PMT-{year}-{(count + 1):D6}";
    }

    private static string GenerateConfirmationNumber()
    {
        return $"CONF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }

    private static DateTime CalculateNextPaymentDate(int dayOfMonth)
    {
        var now = DateTime.UtcNow;
        var nextDate = new DateTime(now.Year, now.Month, Math.Min(dayOfMonth, DateTime.DaysInMonth(now.Year, now.Month)));
        
        if (nextDate <= now)
        {
            nextDate = nextDate.AddMonths(1);
            nextDate = new DateTime(nextDate.Year, nextDate.Month, Math.Min(dayOfMonth, DateTime.DaysInMonth(nextDate.Year, nextDate.Month)));
        }
        
        return nextDate;
    }

    private async Task<PaymentResponseDto> MapToResponseDtoAsync(Payment payment)
    {
        var customerTask = _customerClient.GetCustomerAsync(payment.CustomerId);
        var loanTask = _loanClient.GetLoanAsync(payment.LoanId);

        await Task.WhenAll(customerTask, loanTask);

        return new PaymentResponseDto
        {
            Id = payment.Id,
            PaymentNumber = payment.PaymentNumber,
            LoanId = payment.LoanId,
            CustomerId = payment.CustomerId,
            Customer = await customerTask,
            Loan = await loanTask,
            Amount = payment.Amount,
            PrincipalAmount = payment.PrincipalAmount,
            InterestAmount = payment.InterestAmount,
            EscrowAmount = payment.EscrowAmount,
            LateFeeAmount = payment.LateFeeAmount,
            AdditionalPrincipal = payment.AdditionalPrincipal,
            Status = payment.Status.ToString(),
            PaymentType = payment.PaymentType.ToString(),
            PaymentMethod = payment.PaymentMethod.ToString(),
            ScheduledDate = payment.ScheduledDate,
            DueDate = payment.DueDate,
            ProcessedDate = payment.ProcessedDate,
            ConfirmationNumber = payment.ConfirmationNumber,
            CreatedAt = payment.CreatedAt
        };
    }

    private static PaymentSummaryDto MapToSummaryDto(Payment payment)
    {
        return new PaymentSummaryDto
        {
            Id = payment.Id,
            PaymentNumber = payment.PaymentNumber,
            LoanId = payment.LoanId,
            CustomerId = payment.CustomerId,
            Amount = payment.Amount,
            Status = payment.Status.ToString(),
            PaymentType = payment.PaymentType.ToString(),
            ProcessedDate = payment.ProcessedDate,
            CreatedAt = payment.CreatedAt
        };
    }

    private static PaymentScheduleResponseDto MapScheduleToDto(PaymentSchedule schedule)
    {
        return new PaymentScheduleResponseDto
        {
            Id = schedule.Id,
            LoanId = schedule.LoanId,
            CustomerId = schedule.CustomerId,
            IsAutoPay = schedule.IsAutoPay,
            PreferredPaymentMethod = schedule.PreferredPaymentMethod.ToString(),
            PaymentDayOfMonth = schedule.PaymentDayOfMonth,
            NextPaymentDate = schedule.NextPaymentDate,
            RegularPaymentAmount = schedule.RegularPaymentAmount,
            IsActive = schedule.IsActive
        };
    }

    #endregion
}

using System.ComponentModel.DataAnnotations;
using Payments.API.Models;
using Payments.API.Clients;

namespace Payments.API.DTOs;

// Payment Response DTO
public record PaymentResponseDto
{
    public Guid Id { get; init; }
    public string PaymentNumber { get; init; } = string.Empty;
    public Guid LoanId { get; init; }
    public Guid CustomerId { get; init; }
    
    // Enriched data
    public CustomerDto? Customer { get; init; }
    public LoanDto? Loan { get; init; }
    
    public decimal Amount { get; init; }
    public decimal PrincipalAmount { get; init; }
    public decimal InterestAmount { get; init; }
    public decimal EscrowAmount { get; init; }
    public decimal LateFeeAmount { get; init; }
    public decimal AdditionalPrincipal { get; init; }
    public string Status { get; init; } = string.Empty;
    public string PaymentType { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public DateTime? ScheduledDate { get; init; }
    public DateTime? DueDate { get; init; }
    public DateTime? ProcessedDate { get; init; }
    public string? ConfirmationNumber { get; init; }
    public DateTime CreatedAt { get; init; }
}

// Payment Summary DTO
public record PaymentSummaryDto
{
    public Guid Id { get; init; }
    public string PaymentNumber { get; init; } = string.Empty;
    public Guid LoanId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal Amount { get; init; }
    public string Status { get; init; } = string.Empty;
    public string PaymentType { get; init; } = string.Empty;
    public DateTime? ProcessedDate { get; init; }
    public DateTime CreatedAt { get; init; }
}

// Create Payment DTO
public record CreatePaymentDto
{
    [Required]
    public Guid LoanId { get; init; }

    [Required]
    public Guid CustomerId { get; init; }

    [Required]
    [Range(0.01, 10000000)]
    public decimal Amount { get; init; }

    public PaymentType PaymentType { get; init; } = PaymentType.Regular;

    public PaymentMethod PaymentMethod { get; init; } = PaymentMethod.BankTransfer;

    public DateTime? ScheduledDate { get; init; }

    [Range(0, 10000000)]
    public decimal AdditionalPrincipal { get; init; }

    [StringLength(50)]
    public string? BankAccountLast4 { get; init; }

    [StringLength(500)]
    public string? Notes { get; init; }
}

// Process Payment DTO
public record ProcessPaymentDto
{
    [Required]
    public Guid PaymentId { get; init; }

    public PaymentMethod? PaymentMethod { get; init; }

    [StringLength(50)]
    public string? BankAccountLast4 { get; init; }

    [StringLength(500)]
    public string? Notes { get; init; }
}

// Payment Schedule DTOs
public record PaymentScheduleResponseDto
{
    public Guid Id { get; init; }
    public Guid LoanId { get; init; }
    public Guid CustomerId { get; init; }
    public bool IsAutoPay { get; init; }
    public string PreferredPaymentMethod { get; init; } = string.Empty;
    public int PaymentDayOfMonth { get; init; }
    public DateTime? NextPaymentDate { get; init; }
    public decimal RegularPaymentAmount { get; init; }
    public bool IsActive { get; init; }
}

public record CreatePaymentScheduleDto
{
    [Required]
    public Guid LoanId { get; init; }

    [Required]
    public Guid CustomerId { get; init; }

    public bool IsAutoPay { get; init; }

    public PaymentMethod PreferredPaymentMethod { get; init; } = PaymentMethod.BankTransfer;

    [StringLength(50)]
    public string? BankAccountNumber { get; init; }

    [StringLength(20)]
    public string? RoutingNumber { get; init; }

    [Range(1, 28)]
    public int PaymentDayOfMonth { get; init; } = 1;

    [Required]
    [Range(0.01, 10000000)]
    public decimal RegularPaymentAmount { get; init; }
}

// Late Fee DTO
public record LateFeeResponseDto
{
    public Guid Id { get; init; }
    public Guid LoanId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal Amount { get; init; }
    public DateTime DueDate { get; init; }
    public DateTime? AssessedDate { get; init; }
    public bool IsPaid { get; init; }
    public DateTime? PaidDate { get; init; }
}

// Payment History DTO
public record PaymentHistoryDto
{
    public Guid LoanId { get; init; }
    public string LoanNumber { get; init; } = string.Empty;
    public decimal TotalPaid { get; init; }
    public decimal TotalPrincipalPaid { get; init; }
    public decimal TotalInterestPaid { get; init; }
    public decimal TotalLateFeesPaid { get; init; }
    public int PaymentCount { get; init; }
    public DateTime? LastPaymentDate { get; init; }
    public List<PaymentSummaryDto> Payments { get; init; } = new();
}

// API Response wrapper
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public List<string>? Errors { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> FailResponse(string message, List<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors };
}

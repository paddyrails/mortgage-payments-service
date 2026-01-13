using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Payments.API.Models;

public class Payment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(20)]
    public string PaymentNumber { get; set; } = string.Empty;

    [Required]
    public Guid LoanId { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PrincipalAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal InterestAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal EscrowAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal LateFeeAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AdditionalPrincipal { get; set; }

    [Required]
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    [Required]
    public PaymentType PaymentType { get; set; } = PaymentType.Regular;

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankTransfer;

    public DateTime? ScheduledDate { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? ProcessedDate { get; set; }

    [StringLength(50)]
    public string? ConfirmationNumber { get; set; }

    [StringLength(100)]
    public string? BankAccountLast4 { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(500)]
    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public List<PaymentTransaction> Transactions { get; set; } = new();
}

public enum PaymentStatus
{
    Scheduled = 1,
    Pending = 2,
    Processing = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6,
    Refunded = 7,
    Reversed = 8
}

public enum PaymentType
{
    Regular = 1,
    Extra = 2,
    Payoff = 3,
    LateFee = 4,
    Escrow = 5,
    Partial = 6
}

public enum PaymentMethod
{
    BankTransfer = 1,
    ACH = 2,
    Wire = 3,
    Check = 4,
    DebitCard = 5,
    AutoPay = 6
}

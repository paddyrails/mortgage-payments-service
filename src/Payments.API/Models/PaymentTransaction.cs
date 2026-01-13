using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Payments.API.Models;

public class PaymentTransaction
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid PaymentId { get; set; }

    [Required]
    [StringLength(50)]
    public string TransactionNumber { get; set; } = string.Empty;

    [Required]
    public TransactionType TransactionType { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    public TransactionStatus Status { get; set; }

    [StringLength(100)]
    public string? ExternalReferenceId { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    // Navigation
    public Payment? Payment { get; set; }
}

public enum TransactionType
{
    Debit = 1,
    Credit = 2,
    Refund = 3,
    Reversal = 4,
    Fee = 5
}

public enum TransactionStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Reversed = 4
}

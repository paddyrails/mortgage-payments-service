using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Payments.API.Models;

public class LateFee
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid LoanId { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    public DateTime? AssessedDate { get; set; }

    public bool IsPaid { get; set; }

    public DateTime? PaidDate { get; set; }

    public Guid? PaymentId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

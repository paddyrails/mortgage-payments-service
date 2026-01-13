using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Payments.API.Models;

public class PaymentSchedule
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid LoanId { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    public bool IsAutoPay { get; set; }

    public PaymentMethod PreferredPaymentMethod { get; set; } = PaymentMethod.BankTransfer;

    [StringLength(50)]
    public string? BankAccountNumber { get; set; }

    [StringLength(20)]
    public string? RoutingNumber { get; set; }

    public int PaymentDayOfMonth { get; set; } = 1;

    public DateTime? NextPaymentDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RegularPaymentAmount { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

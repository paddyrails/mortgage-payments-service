using Microsoft.EntityFrameworkCore;
using Payments.API.Models;

namespace Payments.API.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentTransaction> Transactions => Set<PaymentTransaction>();
    public DbSet<PaymentSchedule> PaymentSchedules => Set<PaymentSchedule>();
    public DbSet<LateFee> LateFees => Set<LateFee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PaymentNumber).IsUnique();
            entity.HasIndex(e => e.LoanId);
            entity.HasIndex(e => e.CustomerId);

            entity.HasMany(e => e.Transactions)
                .WithOne(t => t.Payment)
                .HasForeignKey(t => t.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LoanId);
            entity.HasIndex(e => e.CustomerId);
        });

        modelBuilder.Entity<LateFee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LoanId);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var paymentId = Guid.Parse("22220000-0000-0000-0000-000000000001");
        var loanId = Guid.Parse("11110000-0000-0000-0000-000000000001");
        var customerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        modelBuilder.Entity<Payment>().HasData(
            new Payment
            {
                Id = paymentId,
                PaymentNumber = "PMT-2024-000001",
                LoanId = loanId,
                CustomerId = customerId,
                Amount = 5265.27m,
                PrincipalAmount = 1200.00m,
                InterestAmount = 3265.27m,
                EscrowAmount = 800.00m,
                LateFeeAmount = 0,
                AdditionalPrincipal = 0,
                Status = PaymentStatus.Completed,
                PaymentType = PaymentType.Regular,
                PaymentMethod = PaymentMethod.AutoPay,
                DueDate = DateTime.UtcNow.AddMonths(-1),
                ProcessedDate = DateTime.UtcNow.AddMonths(-1).AddDays(1),
                ConfirmationNumber = "CONF-123456",
                BankAccountLast4 = "4567",
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            }
        );

        modelBuilder.Entity<PaymentSchedule>().HasData(
            new PaymentSchedule
            {
                Id = Guid.Parse("33330000-0000-0000-0000-000000000001"),
                LoanId = loanId,
                CustomerId = customerId,
                IsAutoPay = true,
                PreferredPaymentMethod = PaymentMethod.AutoPay,
                PaymentDayOfMonth = 1,
                NextPaymentDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1),
                RegularPaymentAmount = 5265.27m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-3)
            }
        );
    }
}

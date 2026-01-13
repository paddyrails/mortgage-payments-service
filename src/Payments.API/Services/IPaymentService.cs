using Payments.API.DTOs;
using Payments.API.Models;

namespace Payments.API.Services;

public interface IPaymentService
{
    // Payments
    Task<IEnumerable<PaymentSummaryDto>> GetAllPaymentsAsync();
    Task<PaymentResponseDto?> GetPaymentByIdAsync(Guid id);
    Task<IEnumerable<PaymentSummaryDto>> GetPaymentsByLoanAsync(Guid loanId);
    Task<IEnumerable<PaymentSummaryDto>> GetPaymentsByCustomerAsync(Guid customerId);
    Task<PaymentHistoryDto?> GetPaymentHistoryAsync(Guid loanId);
    Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentDto dto);
    Task<PaymentResponseDto?> ProcessPaymentAsync(Guid paymentId);
    Task<bool> CancelPaymentAsync(Guid id);
    
    // Payment Schedule
    Task<PaymentScheduleResponseDto?> GetPaymentScheduleAsync(Guid loanId);
    Task<PaymentScheduleResponseDto> CreatePaymentScheduleAsync(CreatePaymentScheduleDto dto);
    Task<PaymentScheduleResponseDto?> UpdateAutoPayAsync(Guid loanId, bool enabled);
    
    // Late Fees
    Task<IEnumerable<LateFeeResponseDto>> GetLateFeesByLoanAsync(Guid loanId);
    Task<LateFeeResponseDto> AssessLateFeeAsync(Guid loanId, decimal amount);
}

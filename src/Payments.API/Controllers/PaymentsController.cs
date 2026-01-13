using Microsoft.AspNetCore.Mvc;
using Payments.API.DTOs;
using Payments.API.Services;

namespace Payments.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PaymentSummaryDto>>>> GetAll()
    {
        var payments = await _paymentService.GetAllPaymentsAsync();
        return Ok(ApiResponse<IEnumerable<PaymentSummaryDto>>.SuccessResponse(payments));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PaymentResponseDto>>> GetById(Guid id)
    {
        var payment = await _paymentService.GetPaymentByIdAsync(id);
        if (payment == null)
            return NotFound(ApiResponse<PaymentResponseDto>.FailResponse($"Payment {id} not found"));
        return Ok(ApiResponse<PaymentResponseDto>.SuccessResponse(payment));
    }

    [HttpGet("loan/{loanId:guid}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PaymentSummaryDto>>>> GetByLoan(Guid loanId)
    {
        var payments = await _paymentService.GetPaymentsByLoanAsync(loanId);
        return Ok(ApiResponse<IEnumerable<PaymentSummaryDto>>.SuccessResponse(payments));
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PaymentSummaryDto>>>> GetByCustomer(Guid customerId)
    {
        var payments = await _paymentService.GetPaymentsByCustomerAsync(customerId);
        return Ok(ApiResponse<IEnumerable<PaymentSummaryDto>>.SuccessResponse(payments));
    }

    [HttpGet("loan/{loanId:guid}/history")]
    public async Task<ActionResult<ApiResponse<PaymentHistoryDto>>> GetHistory(Guid loanId)
    {
        var history = await _paymentService.GetPaymentHistoryAsync(loanId);
        if (history == null)
            return NotFound(ApiResponse<PaymentHistoryDto>.FailResponse($"Loan {loanId} not found"));
        return Ok(ApiResponse<PaymentHistoryDto>.SuccessResponse(history));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PaymentResponseDto>>> Create([FromBody] CreatePaymentDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<PaymentResponseDto>.FailResponse("Validation failed", errors));
        }

        try
        {
            var payment = await _paymentService.CreatePaymentAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = payment.Id },
                ApiResponse<PaymentResponseDto>.SuccessResponse(payment, "Payment created"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PaymentResponseDto>.FailResponse(ex.Message));
        }
    }

    [HttpPost("{id:guid}/process")]
    public async Task<ActionResult<ApiResponse<PaymentResponseDto>>> Process(Guid id)
    {
        try
        {
            var payment = await _paymentService.ProcessPaymentAsync(id);
            if (payment == null)
                return NotFound(ApiResponse<PaymentResponseDto>.FailResponse($"Payment {id} not found"));
            return Ok(ApiResponse<PaymentResponseDto>.SuccessResponse(payment, "Payment processed"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PaymentResponseDto>.FailResponse(ex.Message));
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(Guid id)
    {
        try
        {
            var result = await _paymentService.CancelPaymentAsync(id);
            if (!result)
                return NotFound(ApiResponse<object>.FailResponse($"Payment {id} not found"));
            return Ok(ApiResponse<object>.SuccessResponse(new { Id = id }, "Payment cancelled"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    // Payment Schedule endpoints
    [HttpGet("schedule/loan/{loanId:guid}")]
    public async Task<ActionResult<ApiResponse<PaymentScheduleResponseDto>>> GetSchedule(Guid loanId)
    {
        var schedule = await _paymentService.GetPaymentScheduleAsync(loanId);
        if (schedule == null)
            return NotFound(ApiResponse<PaymentScheduleResponseDto>.FailResponse("Schedule not found"));
        return Ok(ApiResponse<PaymentScheduleResponseDto>.SuccessResponse(schedule));
    }

    [HttpPost("schedule")]
    public async Task<ActionResult<ApiResponse<PaymentScheduleResponseDto>>> CreateSchedule([FromBody] CreatePaymentScheduleDto dto)
    {
        var schedule = await _paymentService.CreatePaymentScheduleAsync(dto);
        return CreatedAtAction(nameof(GetSchedule), new { loanId = dto.LoanId },
            ApiResponse<PaymentScheduleResponseDto>.SuccessResponse(schedule, "Schedule created"));
    }

    [HttpPatch("schedule/loan/{loanId:guid}/autopay")]
    public async Task<ActionResult<ApiResponse<PaymentScheduleResponseDto>>> UpdateAutoPay(Guid loanId, [FromQuery] bool enabled)
    {
        var schedule = await _paymentService.UpdateAutoPayAsync(loanId, enabled);
        if (schedule == null)
            return NotFound(ApiResponse<PaymentScheduleResponseDto>.FailResponse("Schedule not found"));
        return Ok(ApiResponse<PaymentScheduleResponseDto>.SuccessResponse(schedule, 
            $"AutoPay {(enabled ? "enabled" : "disabled")}"));
    }

    // Late Fee endpoints
    [HttpGet("fees/loan/{loanId:guid}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LateFeeResponseDto>>>> GetLateFees(Guid loanId)
    {
        var fees = await _paymentService.GetLateFeesByLoanAsync(loanId);
        return Ok(ApiResponse<IEnumerable<LateFeeResponseDto>>.SuccessResponse(fees));
    }

    [HttpPost("fees/loan/{loanId:guid}")]
    public async Task<ActionResult<ApiResponse<LateFeeResponseDto>>> AssessLateFee(Guid loanId, [FromQuery] decimal amount)
    {
        try
        {
            var fee = await _paymentService.AssessLateFeeAsync(loanId, amount);
            return Ok(ApiResponse<LateFeeResponseDto>.SuccessResponse(fee, "Late fee assessed"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<LateFeeResponseDto>.FailResponse(ex.Message));
        }
    }
}
